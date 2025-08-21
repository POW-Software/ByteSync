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

    public FileSlicer(ISlicerEncrypter slicerEncrypter, Channel<FileUploaderSlice> availableSlices, 
        SemaphoreSlim semaphoreSlim, ManualResetEvent exceptionOccurred, ILogger<FileSlicer> logger)
    {
        _slicerEncrypter = slicerEncrypter;
        _availableSlices = availableSlices;
        _semaphoreSlim = semaphoreSlim;
        _exceptionOccurred = exceptionOccurred;
        _logger = logger;
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
                progressState.LastException = ex;
            }
            finally
            {
                _semaphoreSlim.Release();   
            }
            _exceptionOccurred.Set();
        }
    }
} 