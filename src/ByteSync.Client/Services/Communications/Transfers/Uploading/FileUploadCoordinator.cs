using System.Threading;
using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class FileUploadCoordinator : IFileUploadCoordinator
{
    private readonly object _syncRoot;
    private readonly Channel<FileUploaderSlice> _availableSlices;
    private readonly ManualResetEvent _uploadingIsFinished;
    private readonly ManualResetEvent _exceptionOccurred;
    private readonly ILogger<FileUploadCoordinator> _logger;
    private const int CHANNEL_CAPACITY = 8;

    public FileUploadCoordinator(ILogger<FileUploadCoordinator> logger)
    {
        _logger = logger;
        _syncRoot = new object();
        var options = new BoundedChannelOptions(CHANNEL_CAPACITY)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = false
        };
        _availableSlices = Channel.CreateBounded<FileUploaderSlice>(options);
        _uploadingIsFinished = new ManualResetEvent(false);
        _exceptionOccurred = new ManualResetEvent(false);
    }

    public object SyncRoot => _syncRoot;
    public Channel<FileUploaderSlice> AvailableSlices => _availableSlices;
    public ManualResetEvent UploadingIsFinished => _uploadingIsFinished;
    public ManualResetEvent ExceptionOccurred => _exceptionOccurred;

    public async Task WaitForCompletionAsync()
    {
        await Task.Run(() => WaitHandle.WaitAny(new WaitHandle[] { _uploadingIsFinished, _exceptionOccurred }));
    }

    public void SetException(Exception exception)
    {
        lock (_syncRoot)
        {
            _logger.LogError(exception, "Upload coordination error");
        }
        _exceptionOccurred.Set();
    }

    public bool HasExceptionOccurred()
    {
        return _exceptionOccurred.WaitOne(0);
    }

    public void Reset()
    {
        _uploadingIsFinished.Reset();
        _exceptionOccurred.Reset();
    }
} 
