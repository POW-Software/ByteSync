using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services;

public class SharedFilesService : ISharedFilesService
{
    private readonly ISharedFilesRepository _sharedFilesRepository;
    private readonly IBlobUrlService _blobUrlService;
    private readonly ICloudflareR2UrlService _cloudflareR2UrlService;
    private readonly ILogger<SharedFilesService> _logger;
    private readonly StorageProvider _storageProvider;

    public SharedFilesService(
        ISharedFilesRepository sharedFilesRepository,
        IBlobUrlService blobUrlService,
        ICloudflareR2UrlService cloudflareR2UrlService,
        IOptions<AppSettings> appSettings,
        ILogger<SharedFilesService> logger)
    {
        _sharedFilesRepository = sharedFilesRepository;
        _blobUrlService = blobUrlService;
        _cloudflareR2UrlService = cloudflareR2UrlService;
        _logger = logger;
        _storageProvider = appSettings.Value.DefaultStorageProvider;
    }
    public async Task AssertFilePartIsUploaded(TransferParameters transferParameters, ICollection<string> recipients)
    {
        var sharedFileDefinition = transferParameters.SharedFileDefinition;
        var partNumber = transferParameters.PartNumber!.Value;

        await _sharedFilesRepository.AddOrUpdate(sharedFileDefinition, sharedFileData =>
        {
            sharedFileData ??= new SharedFileData(sharedFileDefinition, recipients, _storageProvider);
            
            sharedFileData.UploadedPartsNumbers.Add(partNumber);

            return sharedFileData;
        });
    }

    public async Task AssertUploadIsFinished(TransferParameters transferParameters, ICollection<string> recipients)
    {
        var sharedFileDefinition = transferParameters.SharedFileDefinition;
        var totalParts = transferParameters.TotalParts!.Value;

        await _sharedFilesRepository.AddOrUpdate(sharedFileDefinition, sharedFileData =>
        {
            sharedFileData ??= new SharedFileData(sharedFileDefinition, recipients, _storageProvider);

            sharedFileData.TotalParts = totalParts;

            return sharedFileData;
        });
    }

    public async Task AssertFilePartIsDownloaded(Client downloadedBy, TransferParameters transferParameters)
    {
        var sharedFileDefinition = transferParameters.SharedFileDefinition;
        var partNumber = transferParameters.PartNumber!.Value;
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
                await (transferParameters.StorageProvider switch
                {
                    StorageProvider.AzureBlobStorage => _blobUrlService.DeleteBlob(sharedFileDefinition, partNumber),
                    StorageProvider.CloudflareR2     => _cloudflareR2UrlService.DeleteObject(sharedFileDefinition, partNumber),
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
                        StorageProvider.AzureBlobStorage => _blobUrlService.DeleteBlob(sharedFileData.SharedFileDefinition, i),
                        StorageProvider.CloudflareR2     => _cloudflareR2UrlService.DeleteObject(sharedFileData.SharedFileDefinition, i),
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