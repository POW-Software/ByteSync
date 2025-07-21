using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.Transfers;

class FileUploader : IFileUploader
{
    private readonly ISessionService _sessionService;
    private readonly ISlicerEncrypter _slicerEncrypter;
    private readonly IPolicyFactory _policyFactory;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly ILogger<FileUploader> _logger;

    public FileUploader(string? localFileToUpload, MemoryStream? memoryStream, SharedFileDefinition sharedFileDefinition, 
        ISessionService sessionService, ISlicerEncrypter slicerEncrypter, 
        IPolicyFactory policyFactory, IFileTransferApiClient fileTransferApiClient, ILogger<FileUploader> logger)
    {
        if (localFileToUpload == null && memoryStream == null)
        {
            throw new ApplicationException("localFileToUpload and memoryStream are null");
        }

        _sessionService = sessionService;
        _slicerEncrypter = slicerEncrypter;
        _policyFactory = policyFactory;
        _fileTransferApiClient = fileTransferApiClient;
        _logger = logger;
        
        LocalFileToUpload = localFileToUpload;
        MemoryStream = memoryStream;
        SharedFileDefinition = sharedFileDefinition ?? throw new NullReferenceException("SharedFileDefinition is null");

        TotalCreatedSlices = 0;
        TotalUploadedSlices = 0;

        AvailableSlices = Channel.CreateBounded<FileUploaderSlice>(8);

        SyncRoot = new object();

        UploadingIsFinished = new ManualResetEvent(false);
        ExceptionOccurred = new ManualResetEvent(false);
    }

    private object SyncRoot { get; }

    private Channel<FileUploaderSlice> AvailableSlices { get; set; }
    
    private ManualResetEvent UploadingIsFinished { get; set; }
    
    private ManualResetEvent ExceptionOccurred { get; set; }

    private int TotalCreatedSlices { get; set; }
    
    private int TotalUploadedSlices { get; set; }

    public int? MaxSliceLength { get; set; }
    
    private Exception? LastException { get; set; }
    
    private int ConcurrentUploads { get; set; }
    
    private int MaxConcurrentUploads { get; set; }
    
    private SharedFileDefinition SharedFileDefinition { get; set; }
    
    public string? LocalFileToUpload { get; set; }
    
    private MemoryStream? MemoryStream { get; set; }
    
#if DEBUG
    public Action<FileUploaderSlice?>? DebugAfterSliceMethod { get; set; }
    public Action<SharedFileDefinition, FileUploaderSlice>? DebugAfterUploadMethod { get; set; }
#endif

    public Task Upload()
    {
        if (LocalFileToUpload != null)
        {
            return UploadFile();
        }
        else
        {
            return UploadMemoryStream();
        }
    }

    private async Task UploadFile()
    {
        var fileInfo = new FileInfo(LocalFileToUpload!);
        
        PrepareUpload(SharedFileDefinition, fileInfo.Length);
        
        _logger.LogInformation("FileUploader: Starting the E2EE upload of {SharedFileDefinitionId} from {File} ({length} KB)", 
            SharedFileDefinition.Id, LocalFileToUpload, SharedFileDefinition.UploadedFileLength / 1024d);

        _slicerEncrypter.Initialize(fileInfo, SharedFileDefinition);
        
        await ProcessUpload(SharedFileDefinition);
    }

    private async Task UploadMemoryStream()
    {
        PrepareUpload(SharedFileDefinition, MemoryStream!.Length);
        
        _logger.LogInformation("FileUploader: Starting the E2EE upload of {SharedFileDefinitionId} from Memory ({length} KB)", 
            SharedFileDefinition.Id, SharedFileDefinition.UploadedFileLength / 1024d);
        
        _slicerEncrypter.Initialize(MemoryStream!, SharedFileDefinition);
        
        await ProcessUpload(SharedFileDefinition);
    }

    private void PrepareUpload(SharedFileDefinition sharedFileDefinition, long length)
    {
        using (var aes = Aes.Create())
        {
            aes.GenerateIV();
            sharedFileDefinition.IV = aes.IV;
        }
        
        SharedFileDefinition = sharedFileDefinition;

        // AvailableSlices.Clear();

        sharedFileDefinition.UploadedFileLength = length;
    }
    
    private async Task ProcessUpload(SharedFileDefinition sharedFileDefinition)
    {
        for (var i = 0; i < 6; i++)
        {
            _ = Task.Run(UploadAvailableSlice);
        }
        
        await Task.Run(() => SliceAndEncrypt(sharedFileDefinition));
        await Task.Run(() => WaitHandle.WaitAny(new WaitHandle[] { UploadingIsFinished, ExceptionOccurred }));

        _slicerEncrypter.Dispose();

        if (LastException != null)
        {
            if (LocalFileToUpload != null)
            {
                throw new Exception($"An error occured while uploading '{LocalFileToUpload}' / sharedFileDefinition.Id:{sharedFileDefinition.Id}",
                    LastException);
            }
            else
            {
                throw new Exception($"An error occured while uploading a stream / sharedFileDefinition.Id:{sharedFileDefinition.Id}",
                    LastException);
            }
        }

        var totalCreatedSlices = GetTotalCreatedSlices();

        await AssertUploadIsFinished(sharedFileDefinition, totalCreatedSlices);
        
        _logger.LogInformation("FileUploader: E2EE upload of {SharedFileDefinitionId} is finished", SharedFileDefinition.Id);
    }

    private async Task SliceAndEncrypt(SharedFileDefinition sharedFileDefinition)
    {
        try
        {
            if (MaxSliceLength != null)
            {
                _slicerEncrypter.MaxSliceLength = MaxSliceLength.Value;
            }

            var canContinue = true;

            while (canContinue)
            {
                // _slicerSemaphore.Wait();

                if (ExceptionOccurred.WaitOne(0))
                {
                    return;
                }

                var fileUploaderSlice = await _slicerEncrypter.SliceAndEncrypt();

            #if DEBUG
                    if (DebugAfterSliceMethod != null)
                    {
                        DebugAfterSliceMethod.Invoke(fileUploaderSlice);
                    }
                #endif

                if (fileUploaderSlice != null)
                {
                    lock (SyncRoot)
                    {
                        TotalCreatedSlices += 1;
                        // var slice = new FileUploaderSlice(TotalCreatedSlices, fileUploaderSlice);
                        // AvailableSlices.Add(fileUploaderSlice);
                    }

                    await AvailableSlices.Writer.WriteAsync(fileUploaderSlice);

                    // Task.Run(() => UploadAvailableSlice(sharedFileDefinition));
                }
                else
                {
                    AvailableSlices.Writer.Complete();
                    
                    // AvailableSlices.CompleteAdding();
                    
                    // SlicingIsFinished.Set();

                    canContinue = false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SliceAndEncrypt");

            lock (SyncRoot)
            {
                LastException = ex;
            }

            ExceptionOccurred.Set();
        }
    }

    private async Task UploadAvailableSlice()
    {
        while (await AvailableSlices.Reader.WaitToReadAsync())
        {
            if (AvailableSlices.Reader.TryRead(out var slice))
            {
                try
                {
                    var policy = _policyFactory.BuildFileUploadPolicy();
                    var response = await policy.ExecuteAsync(() => DoUpload(slice));

                    if (response != null && !response.GetRawResponse().IsError)
                    {
                        var transferParameters = new TransferParameters
                        {
                            SessionId = SharedFileDefinition.SessionId,
                            SharedFileDefinition = SharedFileDefinition,
                            PartNumber = slice.PartNumber
                        };
                        
                        await _fileTransferApiClient.AssertFilePartIsUploaded(transferParameters);

                        lock (SyncRoot)
                        {
                            TotalUploadedSlices += 1;
                        }
                    }
                    else
                    {
                        throw new Exception("UploadAvailableSlice: unable to get upload url");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "UploadAvailableSlice");

                    lock (SyncRoot)
                    {
                        LastException = ex;
                    }

                    ExceptionOccurred.Set();
                }
            }
        }

        lock (SyncRoot)
        {
            if (TotalUploadedSlices == TotalCreatedSlices)
            {
                UploadingIsFinished.Set();
            }
        }
    }
    
    private async Task<Response<BlobContentInfo>> DoUpload(FileUploaderSlice slice)
    {
        try
        {
            lock (SyncRoot)
            {
                ConcurrentUploads += 1;

                if (ConcurrentUploads > MaxConcurrentUploads)
                {
                    MaxConcurrentUploads = ConcurrentUploads;
                }
            }
            
            var transferParameters = new TransferParameters
            {
                SessionId = SharedFileDefinition.SessionId,
                SharedFileDefinition = SharedFileDefinition,
                PartNumber = slice.PartNumber
            };
            
            var uploadFileUrl = await _fileTransferApiClient.GetUploadFileUrl(transferParameters);

            _logger.LogDebug("UploadAvailableSlice: starting sending slice {number} ({length} KB)", slice.PartNumber, slice.MemoryStream.Length / 1024d);

            var options = new BlobClientOptions();
            options.Retry.NetworkTimeout = TimeSpan.FromMinutes(60);

            Response<BlobContentInfo>? response;
            try
            {
                slice.MemoryStream.Position = 0;
                
                var blob = new BlobClient(new Uri(uploadFileUrl), options);
                response = await blob.UploadAsync(slice.MemoryStream); // (slice.FullName);
                
                #if DEBUG
                    if (DebugAfterUploadMethod != null)
                    {
                        DebugAfterUploadMethod.Invoke(SharedFileDefinition, slice);
                    }
                #endif
            }
            catch (Exception)
            {
                _logger.LogError("Error while uploading slice {number}, sharedFileDefinitionId:{sharedFileDefinitionId} ", slice.PartNumber, SharedFileDefinition.Id);
                throw;
            }

            _logger.LogDebug("UploadAvailableSlice: slice {number} is uploaded", slice.PartNumber);

            return response;
        }
        finally
        {
            lock (SyncRoot)
            {
                ConcurrentUploads -= 1;
            }
        }
    }

    private async Task AssertUploadIsFinished(SharedFileDefinition sharedFileDefinition, int totalParts)
    {
        var transferParameters = new TransferParameters
        {
            SessionId = _sessionService.SessionId!,
            SharedFileDefinition = sharedFileDefinition,
            TotalParts = totalParts
        };
        
        await _fileTransferApiClient.AssertUploadIsFinished(transferParameters);
    }

    public int GetTotalCreatedSlices()
    {
        lock (SyncRoot)
        {
            return TotalCreatedSlices;
        }
    }
    
    public int GetMaxConcurrentUploads()
    {
        lock (SyncRoot)
        {
            return MaxConcurrentUploads;
        }
    }
}