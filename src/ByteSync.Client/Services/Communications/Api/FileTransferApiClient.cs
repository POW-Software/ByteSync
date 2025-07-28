using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Services.Communications.Api;

public class FileTransferApiClient : IFileTransferApiClient
{
    private readonly IApiInvoker _apiInvoker;
    private readonly ILogger<FileTransferApiClient> _logger;

    public FileTransferApiClient(IApiInvoker apiInvoker, ILogger<FileTransferApiClient> logger)
    {
        _apiInvoker = apiInvoker!;
        _logger = logger;
    }
    
    public async Task<string> GetUploadFileUrl(TransferParameters transferParameters)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<string>($"session/{transferParameters.SessionId}/file/getUploadUrl", 
                transferParameters);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting upload file url");
            
            throw;
        }
    }
    
    public async Task<string> GetDownloadFileUrl(TransferParameters transferParameters)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<string>($"session/{transferParameters.SessionId}/file/getDownloadUrl", 
                transferParameters);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting download file url");
            
            throw;
        }
    }
    
    public async Task<FileStorageLocation> GetDownloadFileStorageLocation(TransferParameters transferParameters)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<FileStorageLocation>($"session/{transferParameters.SessionId}/file/getDownloadStorageLocation", 
                transferParameters);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting download file storage location");
            
            throw;
        }
    }
    
    public async Task AssertFilePartIsUploaded(TransferParameters transferParameters)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{transferParameters.SessionId}/file/partUploaded", 
                transferParameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asserting file part is uploaded");
            
            throw;
        }
    }
    
    public async Task AssertUploadIsFinished(TransferParameters transferParameters)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{transferParameters.SessionId}/file/uploadFinished", 
                transferParameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asserting upload is finished");
            
            throw;
        }
    }
    
    public async Task AssertFilePartIsDownloaded(TransferParameters transferParameters)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{transferParameters.SessionId}/file/partDownloaded", 
                transferParameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asserting file part is downloaded");
            
            throw;
        }
    }
    
    public async Task AssertDownloadIsFinished(TransferParameters transferParameters)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{transferParameters.SessionId}/file/downloadFinished", 
                transferParameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asserting download is finished");
            
            throw;
        }
    }
}