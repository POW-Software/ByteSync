using System.Threading;
using System.Threading.Channels;
using Autofac.Features.Indexed;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class FileUploadWorker : IFileUploadWorker
{
    private readonly IPolicyFactory _policyFactory;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly ILogger<FileUploadWorker> _logger;
    private readonly SharedFileDefinition _sharedFileDefinition;
    private readonly ManualResetEvent _exceptionOccurred;
    private readonly ManualResetEvent _uploadingIsFinished;
    private readonly IIndex<StorageProvider, IUploadStrategy> _strategies;
    private readonly SemaphoreSlim _semaphoreSlim;
    
    private CancellationTokenSource CancellationTokenSource { get; }
    
    public FileUploadWorker(IPolicyFactory policyFactory, IFileTransferApiClient fileTransferApiClient,
        SharedFileDefinition sharedFileDefinition, SemaphoreSlim semaphoreSlim, ManualResetEvent exceptionOccurred,
        IIndex<StorageProvider, IUploadStrategy> strategies,
        ManualResetEvent uploadingIsFinished, ILogger<FileUploadWorker> logger)
    {
        _policyFactory = policyFactory;
        _fileTransferApiClient = fileTransferApiClient;
        _sharedFileDefinition = sharedFileDefinition;
        _semaphoreSlim = semaphoreSlim;
        _exceptionOccurred = exceptionOccurred;
        _uploadingIsFinished = uploadingIsFinished;
        _strategies = strategies;
        _logger = logger;
        CancellationTokenSource = new CancellationTokenSource();
    }

    public void StartUploadWorkers(Channel<FileUploaderSlice> availableSlices, int workerCount, UploadProgressState progressState)
    {
        for (var i = 0; i < workerCount; i++)
        {
            _ = Task.Run(() => UploadAvailableSlicesAsync(availableSlices, progressState));
        }
    }

    public async Task UploadAvailableSlicesAsync(Channel<FileUploaderSlice> availableSlices, UploadProgressState progressState)
    {
        while (await availableSlices.Reader.WaitToReadAsync())
        {
            if (availableSlices.Reader.TryRead(out var slice))
            {
                var sliceStart = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    await _semaphoreSlim.WaitAsync();
                    try
                    {
                        progressState.ConcurrentUploads += 1;
                        if (progressState.ConcurrentUploads > progressState.MaxConcurrentUploads)
                        {
                            progressState.MaxConcurrentUploads = progressState.ConcurrentUploads;
                        }
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    }

                    var policy = _policyFactory.BuildFileUploadPolicy();
                    var response = await policy.ExecuteAsync(() => DoUpload(slice));

                    if (response != null && response.IsSuccess)
                    {
                        var transferParameters = new TransferParameters
                        {
                            SessionId = _sharedFileDefinition.SessionId,
                            SharedFileDefinition = _sharedFileDefinition,
                            PartNumber = slice.PartNumber
                        };
                        
                        await policy.ExecuteAsync(async () =>
                        {
                            await _fileTransferApiClient.AssertFilePartIsUploaded(transferParameters);
                            return UploadFileResponse.Success(200);
                        });
                        await _semaphoreSlim.WaitAsync();
                        try
                        {
                            progressState.TotalUploadedSlices += 1;
                            var sliceBytes = slice.MemoryStream?.Length ?? 0L;
                            progressState.TotalUploadedBytes += sliceBytes;
                            var elapsedMs = sliceStart.ElapsedMilliseconds;
                            progressState.LastSliceUploadDurationMs = elapsedMs;
                            progressState.LastSliceUploadedBytes = sliceBytes;
                            progressState.SliceMetrics.Add(new SliceUploadMetric
                            {
                                TaskId = Environment.CurrentManagedThreadId,
                                PartNumber = slice.PartNumber,
                                Bytes = sliceBytes,
                                ElapsedtimeMs = elapsedMs,
                            });
                        }
                        finally
                        {
                            _semaphoreSlim.Release();
                        }
                        _logger.LogInformation(
                            "Slice {PartNumber}: {Bytes} bytes in {DurationMs} ms ({Kbps:F2} kbps)",
                            slice.PartNumber,
                            slice.MemoryStream?.Length ?? 0L,
                            sliceStart.ElapsedMilliseconds,
                            (slice.MemoryStream?.Length ?? 0L) > 0 && sliceStart.ElapsedMilliseconds > 0
                                ? ((slice.MemoryStream?.Length ?? 0L) * 8.0) / sliceStart.ElapsedMilliseconds
                                : 0.0);
                    }
                    else
                    {
                        throw new Exception($"UploadAvailableSlice: unable to get upload url. Status: {response?.StatusCode}, Error: {response?.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "UploadAvailableSlice");
                    
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
                    
                    return;
                }
                finally
                {
                // finalize per-slice metrics

                    var elapsedMs = sliceStart?.ElapsedMilliseconds;
                    await _semaphoreSlim.WaitAsync();
                    try
                    {
                        if (elapsedMs.HasValue)
                        {
                            progressState.LastSliceUploadDurationMs = elapsedMs.Value;
                            progressState.LastSliceUploadedBytes = slice.MemoryStream?.Length ?? 0L;
                        }
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    }
                

                    await _semaphoreSlim.WaitAsync();
                    try
                    {
                        if (progressState.ConcurrentUploads > 0)
                        {
                            progressState.ConcurrentUploads -= 1;
                        }
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    }
                }
            }
        }

        await _semaphoreSlim.WaitAsync();
        try
        {
            if (progressState.TotalUploadedSlices == progressState.TotalCreatedSlices)
            {
                _uploadingIsFinished.Set();
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

	public async Task UploadAvailableSlicesAdaptiveAsync(Channel<FileUploaderSlice> availableSlices, UploadProgressState progressState,
	    IAdaptiveUploadController adaptiveUploadController)
	{
		while (await availableSlices.Reader.WaitToReadAsync())
		{
			if (availableSlices.Reader.TryRead(out var slice))
			{
				try
				{
					var policy = _policyFactory.BuildFileUploadPolicy();
					var startedAt = DateTime.UtcNow;
					var response = await policy.ExecuteAsync(() => DoUpload(slice));

					var elapsed = DateTime.UtcNow - startedAt;
					adaptiveUploadController.RecordUploadResult(elapsed, response != null && response.IsSuccess, slice.PartNumber, response?.StatusCode);

					if (response != null && response.IsSuccess)
					{
						var transferParameters = new TransferParameters
						{
							SessionId = _sharedFileDefinition.SessionId,
							SharedFileDefinition = _sharedFileDefinition,
							PartNumber = slice.PartNumber
						};

						await _fileTransferApiClient.AssertFilePartIsUploaded(transferParameters);
						await _semaphoreSlim.WaitAsync();
						try
						{
							progressState.TotalUploadedSlices += 1;
						}
						finally
						{
							_semaphoreSlim.Release();
						}
					}
					else
					{
						throw new Exception($"UploadAvailableSlice: unable to get upload url. Status: {response?.StatusCode}, Error: {response?.ErrorMessage}");
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "UploadAvailableSlice");

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

					return;
				}
			}
		}

		await _semaphoreSlim.WaitAsync();
		try
		{
			if (progressState.TotalUploadedSlices == progressState.TotalCreatedSlices)
			{
				_uploadingIsFinished.Set();
			}
		}
		finally
		{
			_semaphoreSlim.Release();
		}
	}

    private async Task<UploadFileResponse> DoUpload(FileUploaderSlice slice)
    {
        try
        {
            var transferParameters = new TransferParameters
            {
                SessionId = _sharedFileDefinition.SessionId,
                SharedFileDefinition = _sharedFileDefinition,
                PartNumber = slice.PartNumber
            };
            
            var uploadLocation = await _fileTransferApiClient.GetUploadFileStorageLocation(transferParameters);

            _logger.LogDebug("UploadAvailableSlice: starting sending slice {number} ({length} KB)", 
                slice.PartNumber, slice.MemoryStream.Length / 1024d);
            
            var uploadStrategy = _strategies[uploadLocation.StorageProvider];
            var response = await uploadStrategy.UploadAsync(slice, uploadLocation, CancellationTokenSource.Token);
            
            return response;
        }
        catch (Exception)
        {
            _logger.LogError("Error while uploading slice {number}, sharedFileDefinitionId:{sharedFileDefinitionId} ", 
                slice.PartNumber, _sharedFileDefinition.Id);
            throw;
        }
    }
} 