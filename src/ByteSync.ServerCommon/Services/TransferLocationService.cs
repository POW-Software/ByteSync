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
    private readonly IAzureBlobStorageService _azureBlobStorageService;
    private readonly ICloudflareR2Service _cloudflareR2Service;
    private readonly ILogger<TransferLocationService> _logger;
    private readonly StorageProvider _storageProvider;

    public TransferLocationService(ICloudSessionsRepository cloudSessionsRepository, IAzureBlobStorageService azureBlobStorageService,
        ICloudflareR2Service cloudflareR2Service,
        IOptions<AppSettings> appSettings,
        ILogger<TransferLocationService> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _azureBlobStorageService = azureBlobStorageService;
        _cloudflareR2Service = cloudflareR2Service;
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
                StorageProvider.AzureBlobStorage => await _azureBlobStorageService.GetUploadFileUrl(sharedFileDefinition, partNumber),
                StorageProvider.CloudflareR2 => await _cloudflareR2Service.GetUploadFileUrl(sharedFileDefinition, partNumber),
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
                StorageProvider.AzureBlobStorage => await _azureBlobStorageService.GetDownloadFileUrl(sharedFileDefinition, partNumber),
                StorageProvider.CloudflareR2 => await _cloudflareR2Service.GetDownloadFileUrl(sharedFileDefinition, partNumber),
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