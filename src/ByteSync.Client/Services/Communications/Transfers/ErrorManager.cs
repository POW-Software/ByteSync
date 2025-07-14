using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;

namespace ByteSync.Services.Communications.Transfers;

public class ErrorManager : IErrorManager
{
    
    private bool _isError;
    private readonly object _syncRoot;
    private readonly Channel<int> _mergeChannel;
    private readonly BlockingCollection<int> _downloadQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ErrorManager(object syncRoot, Channel<int> mergeChannel, BlockingCollection<int> downloadQueue, CancellationTokenSource cancellationTokenSource)
    {
        _syncRoot = syncRoot;
        _mergeChannel = mergeChannel;
        _downloadQueue = downloadQueue;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public bool IsError
    {
        get { lock (_syncRoot) { return _isError; } }
    }

    public void SetOnError()
    {
        lock (_syncRoot)
        {
            _isError = true;
            _mergeChannel.Writer.TryComplete();
            _downloadQueue.CompleteAdding();
            _cancellationTokenSource.Cancel();
        }
    }
    
} 