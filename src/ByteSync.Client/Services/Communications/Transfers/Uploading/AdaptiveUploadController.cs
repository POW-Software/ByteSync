using System.Reactive.Linq;
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
    
    private const double MULTIPLIER_2_X = 2.0;
    private const double MULTIPLIER_1_75_X = 1.75;
    private const double MULTIPLIER_1_5_X = 1.5;
    private const double MULTIPLIER_1_25_X = 1.25;
    
    // Thresholds
    private static readonly TimeSpan _upscaleThreshold = TimeSpan.FromSeconds(22);
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
    private readonly object _syncRoot = new();
    
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
        get
        {
            lock (_syncRoot)
            {
                return _currentChunkSizeBytes;
            }
        }
    }
    
    public int CurrentParallelism
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentParallelism;
            }
        }
    }
    
    public int GetNextChunkSizeBytes()
    {
        lock (_syncRoot)
        {
            return _currentChunkSizeBytes;
        }
    }
    
    public void RecordUploadResult(TimeSpan elapsed, bool isSuccess, int partNumber, int? statusCode = null,
        Exception? exception = null, string? fileId = null, long actualBytes = -1)
    {
        lock (_syncRoot)
        {
            EnqueueSample(elapsed, isSuccess, actualBytes);
            
            if (HandleBandwidthReset(isSuccess, statusCode))
            {
                return;
            }
            
            if (_recentDurations.Count < _windowSize)
            {
                return;
            }
            
            var maxElapsed = GetMaxElapsedInWindow();
            
            _logger.LogDebug(
                "Adaptive: file {FileId} maxElapsed={MaxElapsedMs} ms, window={Window}, parallelism={Parallelism}, chunkSize={ChunkKb} KB",
                fileId ?? "-",
                maxElapsed.TotalMilliseconds,
                _windowSize,
                _currentParallelism,
                Math.Round(_currentChunkSizeBytes / 1024d));
            
            if (TryHandleDownscale(maxElapsed, fileId))
            {
                return;
            }
            
            TryHandleUpscale(fileId);
        }
    }
    
    private void EnqueueSample(TimeSpan elapsed, bool isSuccess, long actualBytes)
    {
        _recentDurations.Enqueue(elapsed);
        _recentSuccesses.Enqueue(isSuccess);
        _recentBytes.Enqueue(actualBytes);
        if (isSuccess)
        {
            _successesInWindow += 1;
        }
        
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
    }
    
    private bool HandleBandwidthReset(bool isSuccess, int? statusCode)
    {
        if (!isSuccess && statusCode != null)
        {
            if (statusCode == 429 || statusCode == 500 || statusCode == 503 || statusCode == 507)
            {
                _logger.LogWarning("Adaptive: bandwidth error status {Status}. Resetting chunk size to {InitialKb} KB (was {PrevKb} KB)",
                    statusCode, INITIAL_CHUNK_SIZE_BYTES / 1024, _currentChunkSizeBytes / 1024);
                _currentChunkSizeBytes = Math.Clamp(INITIAL_CHUNK_SIZE_BYTES, MIN_CHUNK_SIZE_BYTES, MAX_CHUNK_SIZE_BYTES);
                ResetWindow();
                
                return true;
            }
        }
        
        return false;
    }
    
    private TimeSpan GetMaxElapsedInWindow()
    {
        var maxElapsed = TimeSpan.Zero;
        foreach (var recentDuration in _recentDurations)
        {
            if (recentDuration > maxElapsed)
            {
                maxElapsed = recentDuration;
            }
        }
        
        return maxElapsed;
    }
    
    private bool TryHandleDownscale(TimeSpan maxElapsed, string? fileId)
    {
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
                
                return true;
            }
            
            var reduced = (int)Math.Max(MIN_CHUNK_SIZE_BYTES, _currentChunkSizeBytes * 0.75);
            if (reduced != _currentChunkSizeBytes)
            {
                _currentChunkSizeBytes = reduced;
                _logger.LogInformation(
                    "Adaptive: file {FileId} Downscale. maxElapsed={MaxElapsedMs} ms > {ThresholdMs} ms. New chunkSize={ChunkKb} KB",
                    fileId ?? "-",
                    maxElapsed.TotalMilliseconds,
                    _downscaleThreshold.TotalMilliseconds,
                    Math.Round(_currentChunkSizeBytes / 1024d));
            }
            
            ResetWindow();
            
            return true;
        }
        
        return false;
    }
    
    private void TryHandleUpscale(string? fileId)
    {
        var recentDurations = _recentDurations.ToArray();
        var recentSuccesses = _recentSuccesses.ToArray();
        var recentBytes = _recentBytes.ToArray();
        var minEligibleBytes = (long)Math.Floor(_currentChunkSizeBytes * 0.9);
        var eligibleIndexes = new List<int>();
        for (var i = 0; i < recentDurations.Length && i < recentSuccesses.Length && i < recentBytes.Length; i++)
        {
            var chunkBytes = recentBytes[i];
            if (chunkBytes < 0)
            {
                chunkBytes = _currentChunkSizeBytes;
            }
            
            if (chunkBytes >= minEligibleBytes)
            {
                eligibleIndexes.Add(i);
            }
        }
        
        if (eligibleIndexes.Count >= _windowSize)
        {
            var start = eligibleIndexes.Count - _windowSize;
            var maxElapsedEligible = TimeSpan.Zero;
            var eligibleSuccesses = 0;
            for (var k = start; k < eligibleIndexes.Count; k++)
            {
                var idx = eligibleIndexes[k];
                var d = recentDurations[idx];
                if (d > maxElapsedEligible)
                {
                    maxElapsedEligible = d;
                }
                
                if (recentSuccesses[idx])
                {
                    eligibleSuccesses++;
                }
            }
            
            if (maxElapsedEligible <= _upscaleThreshold && eligibleSuccesses >= _windowSize)
            {
                var multiplier = GetUpscaleMultiplier(maxElapsedEligible);
                var increased = (int)Math.Round(_currentChunkSizeBytes * multiplier);
                _currentChunkSizeBytes = Math.Clamp(increased, MIN_CHUNK_SIZE_BYTES, MAX_CHUNK_SIZE_BYTES);
                
                _logger.LogInformation(
                    "Adaptive: file {FileId} Upscale. maxElapsed={MaxElapsedMs} ms <= {ThresholdMs} ms. New chunkSize={ChunkKb} KB",
                    fileId ?? "-",
                    maxElapsedEligible.TotalMilliseconds,
                    _upscaleThreshold.TotalMilliseconds,
                    Math.Round(_currentChunkSizeBytes / 1024d));
                
                UpdateParallelismOnUpscale(fileId);
                _currentParallelism = Math.Min(_currentParallelism, MAX_PARALLELISM);
                _windowSize = _currentParallelism;
                ResetWindow();
            }
        }
    }
    
    private double GetUpscaleMultiplier(TimeSpan maxElapsedEligible)
    {
        if (maxElapsedEligible < TimeSpan.FromSeconds(1))
        {
            return MULTIPLIER_2_X;
        }
        
        if (maxElapsedEligible < TimeSpan.FromSeconds(3))
        {
            return MULTIPLIER_1_75_X;
        }
        
        if (maxElapsedEligible < TimeSpan.FromSeconds(10))
        {
            return MULTIPLIER_1_5_X;
        }
        
        return MULTIPLIER_1_25_X;
    }
    
    private void UpdateParallelismOnUpscale(string? fileId)
    {
        if (_currentChunkSizeBytes >= EIGHT_MB)
        {
            var prev = _currentParallelism;
            _currentParallelism = Math.Max(_currentParallelism, 4);
            if (_currentParallelism != prev)
            {
                _logger.LogInformation("Adaptive: file {FileId} Upscale. Increasing parallelism {Prev} -> {Next} due to chunk>=8MB",
                    fileId ?? "-", prev, _currentParallelism);
            }
        }
        else if (_currentChunkSizeBytes >= FOUR_MB)
        {
            var prev = _currentParallelism;
            _currentParallelism = Math.Max(_currentParallelism, 3);
            if (_currentParallelism != prev)
            {
                _logger.LogInformation("Adaptive: file {FileId} Upscale. Increasing parallelism {Prev} -> {Next} due to chunk>=4MB",
                    fileId ?? "-", prev, _currentParallelism);
            }
        }
    }
    
    private void ResetWindow()
    {
        lock (_syncRoot)
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
        lock (_syncRoot)
        {
            _currentChunkSizeBytes = Math.Clamp(INITIAL_CHUNK_SIZE_BYTES, MIN_CHUNK_SIZE_BYTES, MAX_CHUNK_SIZE_BYTES);
            _currentParallelism = MIN_PARALLELISM;
            _windowSize = _currentParallelism;
        }
        
        ResetWindow();
    }
}