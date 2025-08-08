using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Helpers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services;

public class TransferLocationService : ITransferLocationService
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IAzureBlobStorageUrlService _azureBlobStorageUrlService;
    private readonly ICloudflareR2UrlService _cloudflareR2UrlService;
    private readonly ILogger<TransferLocationService> _logger;
    private readonly StorageProvider _storageProvider;

    public TransferLocationService(ICloudSessionsRepository cloudSessionsRepository, IAzureBlobStorageUrlService azureBlobStorageUrlService,
        ICloudflareR2UrlService cloudflareR2UrlService,
        IOptions<AppSettings> appSettings,
        ILogger<TransferLocationService> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _azureBlobStorageUrlService = azureBlobStorageUrlService;
        _cloudflareR2UrlService = cloudflareR2UrlService;
        _storageProvider = appSettings.Value.DefaultStorageProvider;
        _logger = logger;
    }
    
    public async Task<string> GetUploadFileUrl(string sessionId, Client client, TransferParameters transferParameters)
    {
        if (transferParameters.PartNumber == null || transferParameters.PartNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transferParameters.PartNumber), "Part number must be greater than 0");
        }
        
        var sessionMemberData = await _cloudSessionsRepository.GetSessionMember(sessionId, client);
        var sharedFileDefinition = transferParameters.SharedFileDefinition;
        var partNumber = transferParameters.PartNumber.Value;

        bool canGetUrl = IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition);

        if (canGetUrl)
        {
            string uploadUrl = _storageProvider switch
            {
                StorageProvider.AzureBlobStorage => await _azureBlobStorageUrlService.GetUploadFileUrl(sharedFileDefinition, partNumber),
                StorageProvider.CloudflareR2 => await _cloudflareR2UrlService.GetUploadFileUrl(sharedFileDefinition, partNumber),
                _ => throw new NotSupportedException($"Storage provider {_storageProvider} is not supported")
            };

            _logger.LogInformation("GetUploadFileUrl: OK for {@cloudSession} by {@member} {@sharedFileDefinition}",
                sessionMemberData?.CloudSessionData.BuildLog(), sessionMemberData?.BuildLog(), sharedFileDefinition.BuildLog());

            return uploadUrl;
        }
        else
        {
            return null;
        }
    }
    
    public async Task<string> GetDownloadFileUrl(string sessionId, Client client, TransferParameters transferParameters)
    {
        if (transferParameters.PartNumber == null || transferParameters.PartNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transferParameters.PartNumber), "Part number must be greater than 0");
        }
        
        var sessionMemberData = await _cloudSessionsRepository.GetSessionMember(sessionId, client);
        var sharedFileDefinition = transferParameters.SharedFileDefinition;
        var partNumber = transferParameters.PartNumber.Value;

        bool canGetUrl = IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition);

        if (canGetUrl)
        {
            string downloadUrl = _storageProvider switch
            {
                StorageProvider.AzureBlobStorage => await _azureBlobStorageUrlService.GetDownloadFileUrl(sharedFileDefinition, partNumber),
                StorageProvider.CloudflareR2 => await _cloudflareR2UrlService.GetDownloadFileUrl(sharedFileDefinition, partNumber),
                _ => throw new NotSupportedException($"Storage provider {_storageProvider} is not supported")
            };

            return downloadUrl;
        }
        else
        {
            return null;
        }
    }
    
    public bool IsSharedFileDefinitionAllowed(SessionMemberData? sessionMemberData, SharedFileDefinition? sharedFileDefinition)
    {
        bool canGetUrl = false;

        if (sessionMemberData != null && sharedFileDefinition != null)
        {
            canGetUrl = sessionMemberData.CloudSessionData.SessionMembers.Any(smd =>
                Equals(smd.ClientInstanceId, sharedFileDefinition.ClientInstanceId));
        }

        return canGetUrl;
    }
    
}