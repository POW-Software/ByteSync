using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;

namespace ByteSync.Services.Communications.Transfers;

public class ErrorManager : IErrorManager
{
    
    private bool _isError;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly Channel<int> _mergeChannel;
    private readonly BlockingCollection<int> _downloadQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ErrorManager(SemaphoreSlim semaphoreSlim, Channel<int> mergeChannel, BlockingCollection<int> downloadQueue, CancellationTokenSource cancellationTokenSource)
    {
        _semaphoreSlim = semaphoreSlim;
        _mergeChannel = mergeChannel;
        _downloadQueue = downloadQueue;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public bool IsError
    {
        get
        {
            _semaphoreSlim.Wait();
            try
            {
                return _isError;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }

    public void SetOnError()
    {
        _semaphoreSlim.Wait();
        try
        {
            _isError = true;
            _mergeChannel.Writer.TryComplete();
            _downloadQueue.CompleteAdding();
            _cancellationTokenSource.Cancel();
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
    
} 