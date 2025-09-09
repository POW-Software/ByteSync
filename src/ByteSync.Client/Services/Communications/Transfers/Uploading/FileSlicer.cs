using System.Threading;
using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Encryptions;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class FileSlicer : IFileSlicer
{
    private readonly ISlicerEncrypter _slicerEncrypter;
    private readonly ILogger<FileSlicer> _logger;
    private readonly Channel<FileUploaderSlice> _availableSlices;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly ManualResetEvent _exceptionOccurred;
    private readonly IAdaptiveUploadController _adaptiveUploadController;

    public FileSlicer(ISlicerEncrypter slicerEncrypter, Channel<FileUploaderSlice> availableSlices, 
        SemaphoreSlim semaphoreSlim, ManualResetEvent exceptionOccurred, ILogger<FileSlicer> logger, IAdaptiveUploadController adaptiveUploadController)
    {
        _slicerEncrypter = slicerEncrypter;
        _availableSlices = availableSlices;
        _semaphoreSlim = semaphoreSlim;
        _exceptionOccurred = exceptionOccurred;
        _logger = logger;
        _adaptiveUploadController = adaptiveUploadController;
    }

    public int? MaxSliceLength { get; set; }
    
    public async Task SliceAndEncryptAsync(SharedFileDefinition sharedFileDefinition, UploadProgressState progressState, 
        int? maxSliceLength = null)
    {
        try
        {
            if (maxSliceLength != null)
            {
                _slicerEncrypter.MaxSliceLength = maxSliceLength.Value;
            }

            var canContinue = true;

            while (canContinue)
            {
                if (_exceptionOccurred.WaitOne(0))
                {
                    return;
                }

                var fileUploaderSlice = await _slicerEncrypter.SliceAndEncrypt();

                if (fileUploaderSlice != null)
                {
                    await _semaphoreSlim.WaitAsync();
                    try
                    {
                        progressState.TotalCreatedSlices += 1;
                        progressState.TotalCreatedBytes += fileUploaderSlice.MemoryStream.Length;
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    }

                    await _availableSlices.Writer.WriteAsync(fileUploaderSlice);
                }
                else
                {
                    _availableSlices.Writer.Complete();
                    canContinue = false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SliceAndEncrypt");

            await _semaphoreSlim.WaitAsync();
            try
            {
                progressState.Exceptions.Add(ex);
            }
            finally
            {
                _semaphoreSlim.Release();   
            }
            _exceptionOccurred.Set();
            _availableSlices.Writer.TryComplete(ex);
        }
    }

    public async Task SliceAndEncryptAdaptiveAsync(SharedFileDefinition sharedFileDefinition, UploadProgressState progressState)
    {
        try
        {
            _slicerEncrypter.MaxSliceLength = _adaptiveUploadController.CurrentChunkSizeBytes;

            var canContinue = true;
            while (canContinue)
            {
                if (_exceptionOccurred.WaitOne(0))
                {
                    return;
                }

                // Backpressure is handled by a bounded channel; no active polling

                var nextSize = _adaptiveUploadController.GetNextChunkSizeBytes();
                if (nextSize > 0)
                {
                    _slicerEncrypter.MaxSliceLength = nextSize;
                }

                var fileUploaderSlice = await _slicerEncrypter.SliceAndEncrypt();

                if (fileUploaderSlice != null)
                {
                    await _semaphoreSlim.WaitAsync();
                    try
                    {
                        progressState.TotalCreatedSlices += 1;
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    }

                    await _availableSlices.Writer.WriteAsync(fileUploaderSlice);
                }
                else
                {
                    _availableSlices.Writer.Complete();
                    canContinue = false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SliceAndEncryptAdaptive");

            await _semaphoreSlim.WaitAsync();
            try
            {
                progressState.Exceptions.Add(ex);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
            _exceptionOccurred.Set();
            _availableSlices.Writer.TryComplete(ex);
        }
    }
}
