using System.Reactive.Linq;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class AdaptiveUploadController : IAdaptiveUploadController
{
	// Initial configuration
	private const int INITIAL_CHUNK_SIZE_BYTES = 500 * 1024; // 500 KB
	private const int MIN_PARALLELISM = 2;
	private const int MAX_PARALLELISM = 4;

	// Thresholds
	private static readonly TimeSpan UpscaleThreshold = TimeSpan.FromSeconds(25);
	private static readonly TimeSpan DownscaleThreshold = TimeSpan.FromSeconds(30);

	// Chunk size thresholds for parallelism increases
	private const int FOUR_MB = 4 * 1024 * 1024;
	private const int EIGHT_MB = 8 * 1024 * 1024;

	// State
	private int _currentChunkSizeBytes;
	private int _currentParallelism;
	private readonly Queue<TimeSpan> _recentDurations;
	private readonly Queue<int> _recentPartNumbers;
	private readonly Queue<bool> _recentSuccesses;
	private int _successesInWindow;
	private int _windowSize;
	private readonly ILogger<AdaptiveUploadController> _logger;

	public AdaptiveUploadController(ILogger<AdaptiveUploadController> logger, ISessionService sessionService)
	{
		_logger = logger;
		_recentDurations = new Queue<TimeSpan>();
		_recentPartNumbers = new Queue<int>();
		_recentSuccesses = new Queue<bool>();
		ResetState();
		sessionService.SessionObservable.Subscribe(_ => { ResetState(); });
		sessionService.SessionStatusObservable
			.Where(status => status == SessionStatus.Preparation)
			.Subscribe(_ => { ResetState(); });
	}

	public int CurrentChunkSizeBytes => _currentChunkSizeBytes;
	public int CurrentParallelism => _currentParallelism;

	public int GetNextChunkSizeBytes()
	{
		return _currentChunkSizeBytes;
	}

	public void RecordUploadResult(TimeSpan elapsed, bool isSuccess, int partNumber, int? statusCode = null, Exception? exception = null)
	{
		// Track window of last N uploads where N == current parallelism
		_recentDurations.Enqueue(elapsed);
		_recentPartNumbers.Enqueue(partNumber);
		_recentSuccesses.Enqueue(isSuccess);
		if (isSuccess) { _successesInWindow += 1; }
		while (_recentDurations.Count > _windowSize)
		{
			_recentDurations.Dequeue();
			if (_recentPartNumbers.Count > 0)
			{
				_recentPartNumbers.Dequeue();
			}
			if (_recentSuccesses.Count > 0)
			{
				var removedSuccess = _recentSuccesses.Dequeue();
				if (removedSuccess && _successesInWindow > 0)
				{
					_successesInWindow -= 1;
				}
			}
		}

		// If error indicates bandwidth problems, reset chunk size
		if (!isSuccess && statusCode != null)
		{
			if (statusCode == 429 || statusCode == 503 || statusCode == 507)
			{
				_logger.LogWarning("Adaptive: bandwidth error status {Status}. Resetting chunk size to {InitialKb} KB (was {PrevKb} KB)", statusCode, INITIAL_CHUNK_SIZE_BYTES / 1024, _currentChunkSizeBytes / 1024);
				_currentChunkSizeBytes = INITIAL_CHUNK_SIZE_BYTES;
				return;
			}
		}

		if (_recentDurations.Count < _windowSize)
		{
			return; // not enough data yet
		}

		var maxElapsed = TimeSpan.Zero;
		foreach (var d in _recentDurations)
		{
			if (d > maxElapsed) maxElapsed = d;
		}

		_logger.LogDebug(
			"Adaptive: maxElapsedMs={MaxElapsedMs}, parallelism={Parallelism}, chunkKB={ChunkKb}",
			maxElapsed.TotalMilliseconds, _currentParallelism, _currentChunkSizeBytes / 1024);

		// Downscale path first
		if (maxElapsed > DownscaleThreshold)
		{
			// First, reduce the number of parallel upload tasks (minimum = 2)
			if (_currentParallelism > MIN_PARALLELISM)
			{
				_logger.LogInformation(
					"Adaptive: Downscale. Reducing parallelism {Prev} -> {Next}. Resetting window.",
					_currentParallelism, _currentParallelism - 1);
				_currentParallelism -= 1;
				_windowSize = _currentParallelism;
				ResetWindow();
				return;
			}

			// If already at minimum parallelism, reduce chunk size proportionally
			var reduced = (int)Math.Max(64 * 1024, _currentChunkSizeBytes * 0.75);
			if (reduced != _currentChunkSizeBytes)
			{
				_currentChunkSizeBytes = reduced;
				_logger.LogInformation(
					"Adaptive: Downscale. maxElapsedMs={MaxElapsedMs} > {ThresholdMs}. New chunkKB={ChunkKb}.", 
					maxElapsed.TotalMilliseconds, DownscaleThreshold.TotalMilliseconds, _currentChunkSizeBytes / 1024);
			}
			ResetWindow();
			return;
		}

		// Upscale when stable and fast (<= 25s) and all in window were successful
		if (maxElapsed <= UpscaleThreshold && _successesInWindow >= _windowSize)
		{
			// Increase chunk size up to 25% per step, but target towards 25s heuristically
			// Simple rule: +25%
			var increased = (int)(_currentChunkSizeBytes * 1.25);
			_currentChunkSizeBytes = increased;
			_logger.LogInformation(
				"Adaptive: Upscale. maxElapsedMs={MaxElapsedMs} <= {ThresholdMs}. New chunkKB={ChunkKb}.", 
				maxElapsed.TotalMilliseconds, UpscaleThreshold.TotalMilliseconds, _currentChunkSizeBytes / 1024);

			// Increase parallelism at thresholds of chunk size
			if (_currentChunkSizeBytes >= EIGHT_MB)
			{
				var prev = _currentParallelism;
				_currentParallelism = Math.Max(_currentParallelism, 4);
				if (_currentParallelism != prev)
				{
					_logger.LogInformation("Adaptive: Upscale. Increasing parallelism {Prev} -> {Next} due to chunk>=8MB.", prev, _currentParallelism);
				}
			}
			else if (_currentChunkSizeBytes >= FOUR_MB)
			{
				var prev = _currentParallelism;
				_currentParallelism = Math.Max(_currentParallelism, 3);
				if (_currentParallelism != prev)
				{
					_logger.LogInformation("Adaptive: Upscale. Increasing parallelism {Prev} -> {Next} due to chunk>=4MB.", prev, _currentParallelism);
				}
			}
			_currentParallelism = Math.Min(_currentParallelism, MAX_PARALLELISM);
			_windowSize = _currentParallelism;
		}
	}

	private void ResetWindow()
	{
		while (_recentDurations.Count > 0)
		{
			_recentDurations.Dequeue();
		}
		while (_recentPartNumbers.Count > 0)
		{
			_recentPartNumbers.Dequeue();
		}
		while (_recentSuccesses.Count > 0)
		{
			_recentSuccesses.Dequeue();
		}
		_successesInWindow = 0;
	}

	private void ResetState()
	{
		_currentChunkSizeBytes = INITIAL_CHUNK_SIZE_BYTES;
		_currentParallelism = MIN_PARALLELISM;
		_windowSize = _currentParallelism;
		ResetWindow();
	}
}