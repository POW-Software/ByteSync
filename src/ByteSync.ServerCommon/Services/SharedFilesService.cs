using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Services;

public class SharedFilesService : ISharedFilesService
{
    private readonly ISharedFilesRepository _sharedFilesRepository;
    private readonly IAzureBlobStorageUrlService _azureBlobStorageUrlService;
    private readonly ILogger<SharedFilesService> _logger;

    public SharedFilesService(ISharedFilesRepository sharedFilesRepository, IAzureBlobStorageUrlService azureBlobStorageUrlService, ILogger<SharedFilesService> logger)
    {
        _sharedFilesRepository = sharedFilesRepository;
        _azureBlobStorageUrlService = azureBlobStorageUrlService;
        _logger = logger;
    }
    public async Task AssertFilePartIsUploaded(SharedFileDefinition sharedFileDefinition, int partNumber, ICollection<string> recipients)
    {
        await _sharedFilesRepository.AddOrUpdate(sharedFileDefinition, sharedFileData =>
        {
            sharedFileData ??= new SharedFileData(sharedFileDefinition, recipients);
            
            sharedFileData.UploadedPartsNumbers.Add(partNumber);

            return sharedFileData;
        });
    }

    public async Task AssertUploadIsFinished(SharedFileDefinition sharedFileDefinition, int totalParts, ICollection<string> recipients)
    {
        await _sharedFilesRepository.AddOrUpdate(sharedFileDefinition, sharedFileData =>
        {
            sharedFileData ??= new SharedFileData(sharedFileDefinition, recipients);

            sharedFileData.TotalParts = totalParts;

            return sharedFileData;
        });
    }

    public async Task AssertFilePartIsDownloaded(SharedFileDefinition sharedFileDefinition, Client downloadedBy, int partNumber)
    {
        bool deleteBlob = false;
        bool unregister = false;
        
        await _sharedFilesRepository.AddOrUpdate(sharedFileDefinition, sharedFileData =>
        {
            if (sharedFileData == null)
            {
                throw new Exception("SharedFileData should not be null");
            }

            sharedFileData.SetDownloadedBy(downloadedBy.ClientInstanceId, partNumber);
            
            if (sharedFileData.IsPartFullyDownloaded(partNumber))
            {
                deleteBlob = true;
            }

            if (sharedFileData.IsFullyDownloaded)
            {
                unregister = true;
            }

            return sharedFileData;
        });

        if (deleteBlob)
        {
            try
            {
                await _azureBlobStorageUrlService.DeleteObject(sharedFileDefinition, partNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not delete blob for SharedFileDefinition:{Id} / PartNumber:{PartNumber}", sharedFileDefinition.Id, partNumber);
            }
        }

        if (unregister)
        {
            try
            {
                await _sharedFilesRepository.Forget(sharedFileDefinition);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not unregister SharedFileDefinition:{Id}", sharedFileDefinition.Id);
            }
        }
    }

    public async Task ClearSession(string sessionId)
    {
        var sharedFileDatas = await _sharedFilesRepository.Clear(sessionId);

        foreach (var sharedFileData in sharedFileDatas)
        {
            for (int i = 1; i <= sharedFileData.TotalParts; i++)
            {
                try
                {
                    await _azureBlobStorageUrlService.DeleteObject(sharedFileData.SharedFileDefinition, i);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during clearing Session {sessionId} file", sessionId);
                }
            }
        }
    }
}