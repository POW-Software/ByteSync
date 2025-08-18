using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services;

public class SharedFilesService : ISharedFilesService
{
    private readonly ISharedFilesRepository _sharedFilesRepository;
    private readonly IAzureBlobStorageService _azureBlobStorageService;
    private readonly ICloudflareR2Service _cloudflareR2Service;
    private readonly AppSettings _appSettings;
    private readonly ILogger<SharedFilesService> _logger;

    public SharedFilesService(
        ISharedFilesRepository sharedFilesRepository,
        IAzureBlobStorageService azureBlobStorageService,
        ICloudflareR2Service cloudflareR2Service,
        ILogger<SharedFilesService> logger,
        IOptions<AppSettings> appSettings)
    {
        _sharedFilesRepository = sharedFilesRepository;
        _azureBlobStorageService = azureBlobStorageService;
        _cloudflareR2Service = cloudflareR2Service;
        _logger = logger;
        _appSettings = appSettings.Value;
    }
    
    public async Task AssertFilePartIsUploaded(TransferParameters transferParameters, ICollection<string> recipients)
    {
        var sharedFileDefinition = transferParameters.SharedFileDefinition;
        var partNumber = transferParameters.PartNumber!.Value;
        var storageProvider = transferParameters.StorageProvider;
        
        await _sharedFilesRepository.AddOrUpdate(sharedFileDefinition, sharedFileData =>
        {
            sharedFileData ??= new SharedFileData(sharedFileDefinition, recipients, storageProvider);
            
            sharedFileData.UploadedPartsNumbers.Add(partNumber);

            return sharedFileData;
        });
    }

    public async Task AssertUploadIsFinished(TransferParameters transferParameters, ICollection<string> recipients)
    {
        var sharedFileDefinition = transferParameters.SharedFileDefinition;
        var totalParts = transferParameters.TotalParts!.Value;
        var storageProvider = transferParameters.StorageProvider;
        await _sharedFilesRepository.AddOrUpdate(sharedFileDefinition, sharedFileData =>
        {
            sharedFileData ??= new SharedFileData(sharedFileDefinition, recipients, storageProvider);

            sharedFileData.TotalParts = totalParts;

            return sharedFileData;
        });
    }

    public async Task AssertFilePartIsDownloaded(Client downloadedBy, TransferParameters transferParameters)
    {
        var sharedFileDefinition = transferParameters.SharedFileDefinition;
        var partNumber = transferParameters.PartNumber!.Value;
        bool objectDeletable = false;
        bool unregister = false;
        
        await _sharedFilesRepository.AddOrUpdate(sharedFileDefinition, sharedFileData =>
        {
            if (sharedFileData == null)
            {
                throw new Exception("SharedFileData should not be null");
            }

            sharedFileData.SetDownloadedBy(downloadedBy.ClientInstanceId, partNumber);
            
            objectDeletable = sharedFileData.IsPartFullyDownloaded(partNumber);
            
            if (sharedFileData.IsFullyDownloaded)
            {
                unregister = true;
            }

            return sharedFileData;
        });

        if ((objectDeletable) && (!_appSettings.RetainFilesAfterTransfer))
        {
            try
            {
                await (transferParameters.StorageProvider switch
                {
                    StorageProvider.AzureBlobStorage => _azureBlobStorageService.DeleteObject(sharedFileDefinition, partNumber),
                    StorageProvider.CloudflareR2     => _cloudflareR2Service.DeleteObject(sharedFileDefinition, partNumber),
                    _ => throw new NotSupportedException($"Storage provider {transferParameters.StorageProvider} is not supported")
                });
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
                    await (sharedFileData.StorageProvider switch
                    {
                        StorageProvider.AzureBlobStorage => _azureBlobStorageService.DeleteObject(sharedFileData.SharedFileDefinition, i),
                        StorageProvider.CloudflareR2     => _cloudflareR2Service.DeleteObject(sharedFileData.SharedFileDefinition, i),
                        _ => throw new NotSupportedException($"Storage provider {sharedFileData.StorageProvider} is not supported")
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during clearing Session {sessionId} file", sessionId);
                }
            }
        }
    }
}