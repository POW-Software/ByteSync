using System;
using System.Reactive.Linq;
using ByteSync.Business.Arguments;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class AdaptiveUploadController : IAdaptiveUploadController
{
	// Initial configuration
	private const int INITIAL_CHUNK_SIZE_BYTES = 500 * 1024; // 500 KB
	private const int MIN_CHUNK_SIZE_BYTES = 64 * 1024; // 64 KB
	private const int MAX_CHUNK_SIZE_BYTES = 16 * 1024 * 1024; // 16 MB
	private const int MIN_PARALLELISM = 2;
	private const int MAX_PARALLELISM = 4;

	// Thresholds
	private static readonly TimeSpan _upscaleThreshold = TimeSpan.FromSeconds(25);
	private static readonly TimeSpan _downscaleThreshold = TimeSpan.FromSeconds(30);

	// Chunk size thresholds for parallelism increases
	private const int FOUR_MB = 4 * 1024 * 1024;
	private const int EIGHT_MB = 8 * 1024 * 1024;

	// State
	private int _currentChunkSizeBytes;
	private int _currentParallelism;
    private readonly Queue<TimeSpan> _recentDurations;
    private readonly Queue<bool> _recentSuccesses;
    private readonly Queue<long> _recentBytes;
    private int _successesInWindow;
    private int _windowSize;
    private readonly ILogger<AdaptiveUploadController> _logger;
    private readonly object _sync = new object();
    
    private static readonly bool FORCE_UPSCALE = DebugArguments.UploadForceUpscale;
    private static readonly bool FORCE_DOWNSCALE = DebugArguments.UploadForceDownscale;
    private bool _forceDownscaleApplied = false;

	public AdaptiveUploadController(ILogger<AdaptiveUploadController> logger, ISessionService sessionService)
	{
		_logger = logger;
        _recentDurations = new Queue<TimeSpan>();
        _recentSuccesses = new Queue<bool>();
        _recentBytes = new Queue<long>();
		ResetState();
		sessionService.SessionObservable.Subscribe(_ => { ResetState(); });
		sessionService.SessionStatusObservable
			.Where(status => status == SessionStatus.Preparation)
			.Subscribe(_ => { ResetState(); });
	}

	public int CurrentChunkSizeBytes
	{
		get { lock (_sync) { return _currentChunkSizeBytes; } }
	}

	public int CurrentParallelism
	{
		get { lock (_sync) { return _currentParallelism; } }
	}

	public int GetNextChunkSizeBytes()
	{
		lock (_sync)
		{
			return _currentChunkSizeBytes;
		}
	}

    public void RecordUploadResult(TimeSpan elapsed, bool isSuccess, int partNumber, int? statusCode = null, Exception? exception = null, string? fileId = null, long actualBytes = -1)
    {
        lock (_sync)
        {
            // Forced downscale (once), only after several upscales: trigger when chunk >= 4 MB, drop to 512 KB
            if (FORCE_DOWNSCALE && !_forceDownscaleApplied && _currentChunkSizeBytes >= FOUR_MB)
            {
                var prevKb = _currentChunkSizeBytes / 1024;
                _currentChunkSizeBytes = Math.Max(512 * 1024, MIN_CHUNK_SIZE_BYTES);
                _forceDownscaleApplied = true;
                _logger.LogInformation(
                    "Adaptive: file {FileId} FORCE Downscale (>=4MB): chunkKB {PrevKb}->{NewKb}. Resetting window",
                    fileId ?? "-", prevKb, _currentChunkSizeBytes / 1024);
                ResetWindow();
                return;
            }

            if (FORCE_UPSCALE)
            {
                var prevKb = _currentChunkSizeBytes / 1024;
                var prevPar = _currentParallelism;
                var increased = (int)Math.Round(_currentChunkSizeBytes * 1.6);
                _currentChunkSizeBytes = Math.Clamp(increased, MIN_CHUNK_SIZE_BYTES, MAX_CHUNK_SIZE_BYTES);

                if (_currentChunkSizeBytes >= EIGHT_MB)
                {
                    _currentParallelism = Math.Max(_currentParallelism, 4);
                }
                else if (_currentChunkSizeBytes >= FOUR_MB)
                {
                    _currentParallelism = Math.Max(_currentParallelism, 3);
                }
                _currentParallelism = Math.Min(_currentParallelism, MAX_PARALLELISM);
                _windowSize = _currentParallelism;

                _logger.LogInformation(
                    "Adaptive: file {FileId} FORCE Upscale enabled. chunkKB {PrevKb}->{NewKb}, parallelism {PrevPar}->{NewPar}",
                    fileId ?? "-", prevKb, _currentChunkSizeBytes / 1024, prevPar, _currentParallelism);

                ResetWindow();
                return;
            }
            _recentDurations.Enqueue(elapsed);
            _recentSuccesses.Enqueue(isSuccess);
            _recentBytes.Enqueue(actualBytes);
            if (isSuccess) { _successesInWindow += 1; }
            while (_recentDurations.Count > _windowSize)
            {
                _recentDurations.Dequeue();
                if (_recentSuccesses.Count > 0)
                {
                    var removedSuccess = _recentSuccesses.Dequeue();
                    if (removedSuccess && _successesInWindow > 0)
                    {
                        _successesInWindow -= 1;
                    }
                }
                if (_recentBytes.Count > 0)
                {
                    _recentBytes.Dequeue();
                }
            }

			if (!isSuccess && statusCode != null)
			{
				if (statusCode == 429 || statusCode == 500 || statusCode == 503 || statusCode == 507)
				{
					_logger.LogWarning("Adaptive: bandwidth error status {Status}. Resetting chunk size to {InitialKb} KB (was {PrevKb} KB)", statusCode, INITIAL_CHUNK_SIZE_BYTES / 1024, _currentChunkSizeBytes / 1024);
					_currentChunkSizeBytes = Math.Clamp(INITIAL_CHUNK_SIZE_BYTES, MIN_CHUNK_SIZE_BYTES, MAX_CHUNK_SIZE_BYTES);
					ResetWindow();
					return;
				}
			}

			if (_recentDurations.Count < _windowSize)
			{
				return;
			}

			var maxElapsed = TimeSpan.Zero;
			foreach (var d in _recentDurations)
			{
				if (d > maxElapsed) maxElapsed = d;
			}


			_logger.LogDebug(
				"Adaptive: file {FileId} maxElapsedMs={MaxElapsedMs}, window={Window}, parallelism={Parallelism}, chunkKB={ChunkKb}",
				fileId ?? "-",
				maxElapsed.TotalMilliseconds,
				_windowSize,
				_currentParallelism,
				_currentChunkSizeBytes / 1024);

			if (maxElapsed > _downscaleThreshold)
			{
				if (_currentParallelism > MIN_PARALLELISM)
				{
					_logger.LogInformation(
						"Adaptive: file {FileId} Downscale. Reducing parallelism {Prev} -> {Next}. Resetting window (window before {WindowBefore})",
						fileId ?? "-",
						_currentParallelism, _currentParallelism - 1,
						_windowSize);
					_currentParallelism -= 1;
					_windowSize = _currentParallelism;
					ResetWindow();
					return;
				}

				var reduced = (int)Math.Max(MIN_CHUNK_SIZE_BYTES, _currentChunkSizeBytes * 0.75);
				if (reduced != _currentChunkSizeBytes)
				{
					_currentChunkSizeBytes = reduced;
					_logger.LogInformation(
						"Adaptive: file {FileId} Downscale. maxElapsedMs={MaxElapsedMs} > {ThresholdMs}. New chunkKB={ChunkKb}", 
						fileId ?? "-",
						maxElapsed.TotalMilliseconds, _downscaleThreshold.TotalMilliseconds, _currentChunkSizeBytes / 1024);
				}
				ResetWindow();
				return;
			}

            // Upscale: only consider samples with size >= ~current chunk size
            var durationsArr = _recentDurations.ToArray();
            var successesArr = _recentSuccesses.ToArray();
            var bytesArr = _recentBytes.ToArray();
            var minEligibleBytes = (long)Math.Floor(_currentChunkSizeBytes * 0.9);
            var eligibleIdx = new List<int>();
            for (int i = 0; i < durationsArr.Length && i < successesArr.Length && i < bytesArr.Length; i++)
            {
                var b = bytesArr[i];
                if (b < 0) b = _currentChunkSizeBytes; // unknown -> assume eligible
                if (b >= minEligibleBytes)
                {
                    eligibleIdx.Add(i);
                }
            }
            if (eligibleIdx.Count >= _windowSize)
            {
                var start = eligibleIdx.Count - _windowSize;
                var maxElapsedEligible = TimeSpan.Zero;
                var eligibleSuccesses = 0;
                for (int k = start; k < eligibleIdx.Count; k++)
                {
                    var idx = eligibleIdx[k];
                    var d = durationsArr[idx];
                    if (d > maxElapsedEligible) maxElapsedEligible = d;
                    if (successesArr[idx]) eligibleSuccesses++;
                }

                if (maxElapsedEligible <= _upscaleThreshold && eligibleSuccesses >= _windowSize)
                {
                    var increased = (int)(_currentChunkSizeBytes * 1.25);
                    _currentChunkSizeBytes = Math.Clamp(increased, MIN_CHUNK_SIZE_BYTES, MAX_CHUNK_SIZE_BYTES);
                    _logger.LogInformation(
                        "Adaptive: file {FileId} Upscale. maxElapsedMs={MaxElapsedMs} <= {ThresholdMs}. New chunkKB={ChunkKb}", 
                        fileId ?? "-",
                        maxElapsedEligible.TotalMilliseconds, _upscaleThreshold.TotalMilliseconds, _currentChunkSizeBytes / 1024);

                    if (_currentChunkSizeBytes >= EIGHT_MB)
                    {
                        var prev = _currentParallelism;
                        _currentParallelism = Math.Max(_currentParallelism, 4);
                        if (_currentParallelism != prev)
                        {
                            _logger.LogInformation("Adaptive: file {FileId} Upscale. Increasing parallelism {Prev} -> {Next} due to chunk>=8MB", fileId ?? "-", prev, _currentParallelism);
                        }
                    }
                    else if (_currentChunkSizeBytes >= FOUR_MB)
                    {
                        var prev = _currentParallelism;
                        _currentParallelism = Math.Max(_currentParallelism, 3);
                        if (_currentParallelism != prev)
                        {
                            _logger.LogInformation("Adaptive: file {FileId} Upscale. Increasing parallelism {Prev} -> {Next} due to chunk>=4MB", fileId ?? "-", prev, _currentParallelism);
                        }
                    }
                    _currentParallelism = Math.Min(_currentParallelism, MAX_PARALLELISM);
                    _windowSize = _currentParallelism;
                    ResetWindow();
                }
            }
        }
    }

	private void ResetWindow()
	{
		lock (_sync)
		{
			while (_recentDurations.Count > 0)
			{
				_recentDurations.Dequeue();
			}
            while (_recentSuccesses.Count > 0)
            {
                _recentSuccesses.Dequeue();
            }
            while (_recentBytes.Count > 0)
            {
                _recentBytes.Dequeue();
            }
            _successesInWindow = 0;
        }
    }

    private void ResetState()
    {
        lock (_sync)
        {
            _currentChunkSizeBytes = Math.Clamp(INITIAL_CHUNK_SIZE_BYTES, MIN_CHUNK_SIZE_BYTES, MAX_CHUNK_SIZE_BYTES);
            _currentParallelism = MIN_PARALLELISM;
            _windowSize = _currentParallelism;
            _forceDownscaleApplied = false;
        }
        ResetWindow();
    }
}
