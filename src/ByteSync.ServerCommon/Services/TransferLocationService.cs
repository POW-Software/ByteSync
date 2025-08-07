using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Helpers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services;

public class TransferLocationService : ITransferLocationService
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IBlobUrlService _blobUrlService;
    private readonly ICloudflareR2UrlService _cloudflareR2UrlService;
    private readonly ILogger<TransferLocationService> _logger;
    private readonly StorageProvider _storageProvider;

    public TransferLocationService(ICloudSessionsRepository cloudSessionsRepository, IBlobUrlService blobUrlService,
        ICloudflareR2UrlService cloudflareR2UrlService,
        IOptions<AppSettings> appSettings,
        ILogger<TransferLocationService> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _blobUrlService = blobUrlService;
        _cloudflareR2UrlService = cloudflareR2UrlService;
        _storageProvider = appSettings.Value.DefaultStorageProvider;
        _logger = logger;
    }
    
    public async Task<string> GetUploadFileUrl(string sessionId, Client client,
        SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        if (partNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(partNumber), "Part number must be greater than 0");
        }
        
        var sessionMemberData = await _cloudSessionsRepository.GetSessionMember(sessionId, client);

        bool canGetUrl = IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition);

        if (canGetUrl)
        {
            string uploadUrl = _storageProvider switch
            {
                StorageProvider.AzureBlobStorage => await _blobUrlService.GetUploadFileUrl(sharedFileDefinition, partNumber),
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
    
    public async Task<string> GetDownloadFileUrl(string sessionId, Client client,
        SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var sessionMemberData = await _cloudSessionsRepository.GetSessionMember(sessionId, client);

        bool canGetUrl = IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition);

        if (canGetUrl)
        {
            string downloadUrl = _storageProvider switch
            {
                StorageProvider.AzureBlobStorage => await _blobUrlService.GetDownloadFileUrl(sharedFileDefinition, partNumber),
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