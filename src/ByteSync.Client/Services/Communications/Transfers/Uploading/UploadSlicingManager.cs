using System.Threading;
using System.Threading.Channels;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class UploadSlicingManager : IUploadSlicingManager
{
    private readonly Channel<Func<Task>> _queue;
    private readonly List<Task> _workers;
    private readonly ILogger<UploadSlicingManager> _logger;
    private const int MAX_CONCURRENT_SLICES = 2;
    private int _generation;
    private readonly ISessionService _sessionService;

    public UploadSlicingManager(ILogger<UploadSlicingManager> logger, ISessionService sessionService)
    {
        _logger = logger;
        _queue = Channel.CreateUnbounded<Func<Task>>();
        _workers = new List<Task>();
        _generation = 0;
        _sessionService = sessionService;
        _sessionService.SessionObservable.Subscribe(_ =>
        {
            try
            {
                Reset();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadSlicingManager reset on session change failed");
            }
        });
        StartWorkers();
    }

    public Task Enqueue(Func<Task> startSlicingAsync)
    {
        var enqueuedGeneration = _generation;
        return _queue.Writer.WriteAsync(async () =>
        {
            if (enqueuedGeneration != _generation)
            {
                return;
            }
            await startSlicingAsync();
        }).AsTask();
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
                await foreach (var work in _queue.Reader.ReadAllAsync())
                {
                    try
                    {
                        await work();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Slicing worker error");
                    }
                }
            }));
        }
    }
}


