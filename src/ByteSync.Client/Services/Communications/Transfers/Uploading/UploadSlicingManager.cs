using System.Threading;
using System.Threading.Channels;
using System.Reactive.Linq;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class UploadSlicingManager : IUploadSlicingManager
{
    private readonly ISessionService _sessionService;
    private readonly IFileSlicerFactory _fileSlicerFactory;
    private readonly ILogger<UploadSlicingManager> _logger;
    
    private readonly Channel<Func<Task>> _queue;
    private readonly List<Task> _workers;
    private readonly List<IDisposable> _subscriptions = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private const int MAX_CONCURRENT_SLICES = 2;
    private int _generation;


    public UploadSlicingManager(IFileSlicerFactory fileSlicerFactory, 
        ISessionService sessionService,
        ILogger<UploadSlicingManager> logger)
    {
        _logger = logger;
        _fileSlicerFactory = fileSlicerFactory;
        _queue = Channel.CreateUnbounded<Func<Task>>();
        _workers = new List<Task>();
        _generation = 0;
        _sessionService = sessionService;
        _subscriptions.Add(_sessionService.SessionObservable.Subscribe(_ =>
        {
            try
            {
                Reset();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadSlicingManager reset on session change failed");
            }
        }));
        _subscriptions.Add(_sessionService.SessionStatusObservable
            .Where(status => status == SessionStatus.Preparation)
            .Subscribe(_ =>
            {
                try
                {
                    Reset();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "UploadSlicingManager reset on session status change failed");
                }
            }));
        StartWorkers();
    }

    public Task<UploadProgressState> Enqueue(
        SharedFileDefinition sharedFileDefinition,
        ISlicerEncrypter slicerEncrypter,
        Channel<FileUploaderSlice> availableSlices,
        SemaphoreSlim semaphoreSlim,
        ManualResetEvent exceptionOccurred,
        IAdaptiveUploadController adaptiveUploadController)
    {
        var enqueuedGeneration = _generation;
        var progressState = new UploadProgressState();
        progressState.StartTimeUtc = DateTimeOffset.UtcNow;

        return _queue.Writer.WriteAsync(async () =>
        {
            if (enqueuedGeneration != _generation)
            {
                return;
            }
            
            var fileSlicer = _fileSlicerFactory.Create(slicerEncrypter, availableSlices, semaphoreSlim, exceptionOccurred);
            
            // var slicer = new FileSlicer(
            //     slicerEncrypter,
            //     availableSlices,
            //     semaphoreSlim,
            //     exceptionOccurred,
            //     _fileSlicerLogger,
            //     adaptiveUploadController);
            await fileSlicer.SliceAndEncryptAdaptiveAsync(sharedFileDefinition, progressState);
            
        }).AsTask().ContinueWith(_ => progressState);
    }

    public void Reset()
    {
        Interlocked.Increment(ref _generation);
    }

    private void StartWorkers()
    {
        for (var i = 0; i < MAX_CONCURRENT_SLICES; i++)
        {
            _workers.Add(Task.Run(async () =>
            {
            await foreach (var work in _queue.Reader.ReadAllAsync(_cancellationTokenSource.Token))
            {
                try
                {
                    await work();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Slicing worker error");
                }
            }
                
            }));
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();
        
        _cancellationTokenSource.CancelAsync();
        
        _queue.Writer.TryComplete();
        
        await Task.WhenAll(_workers).ConfigureAwait(false);
        
        _cancellationTokenSource.Dispose();
    }
}