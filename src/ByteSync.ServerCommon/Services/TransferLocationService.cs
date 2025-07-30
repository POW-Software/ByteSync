using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Helpers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Services;

public class TransferLocationService : ITransferLocationService
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IBlobUrlService _blobUrlService;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ISharedFilesService _sharedFilesService;
    private readonly IUsageStatisticsService _usageStatisticsService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ILogger<TransferLocationService> _logger;

    public TransferLocationService(ICloudSessionsRepository cloudSessionsRepository, IBlobUrlService blobUrlService,
        IInvokeClientsService invokeClientsService, 
        ISharedFilesService sharedFilesService, IUsageStatisticsService usageStatisticsService, 
        ISynchronizationService synchronizationService,
        ILogger<TransferLocationService> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _blobUrlService = blobUrlService;
        _invokeClientsService = invokeClientsService;
        _sharedFilesService = sharedFilesService;
        _usageStatisticsService = usageStatisticsService;
        _synchronizationService = synchronizationService;
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
            string uploadUrl = await _blobUrlService.GetUploadFileUrl(sharedFileDefinition, partNumber);

            _logger.LogInformation("GetUploadFileUrl: OK for {@cloudSession} by {@member} {@sharedFileDefinition}",
                sessionMemberData?.CloudSessionData.BuildLog(), sessionMemberData?.BuildLog(), sharedFileDefinition.BuildLog());

            return uploadUrl;
        }
        else
        {
            return null;
        }
    }
    
    public async Task<FileStorageLocation> GetUploadFileStorageLocation(string sessionId, Client client,
        TransferParameters transferParameters, StorageProvider storageProvider)
    {
        var url = await GetUploadFileUrl(
            sessionId,
            client,
            transferParameters.SharedFileDefinition,
            transferParameters.PartNumber!.Value
        );
        return new FileStorageLocation(url, storageProvider);
    }
    
    public async Task<string> GetDownloadFileUrl(string sessionId, Client client,
        SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var sessionMemberData = await _cloudSessionsRepository.GetSessionMember(sessionId, client);

        bool canGetUrl = IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition);

        if (canGetUrl)
        {
            string uploadUrl = await _blobUrlService.GetDownloadFileUrl(sharedFileDefinition, partNumber);

            return uploadUrl;
        }
        else
        {
            return null;
        }
    }

    public async Task<FileStorageLocation> GetDownloadFileStorageLocation(string sessionId, Client client,
        TransferParameters transferParameters, StorageProvider storageProvider)
    {
        var url = await GetDownloadFileUrl(
            sessionId,
            client,
            transferParameters.SharedFileDefinition,
            transferParameters.PartNumber!.Value
        );
        return new FileStorageLocation(url, storageProvider);
    }
    
    public async Task AssertUploadIsFinished(string sessionId, Client client, TransferParameters transferParameters)
    {
        var session = await _cloudSessionsRepository.Get(sessionId);
        var sessionMemberData = session?.FindMember(client.ClientInstanceId);
        var sharedFileDefinition = transferParameters.SharedFileDefinition;
        var totalParts = transferParameters.TotalParts!.Value;

        if (sessionMemberData != null && IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition))
        {
            _logger.LogInformation("AssertUploadIsFinished: {cloudSession} {sharedFileDefinition}", sessionId, sharedFileDefinition.Id);

            if (sharedFileDefinition.IsInventory || sharedFileDefinition.IsSynchronizationStartData || sharedFileDefinition.IsProfileDetails)
            {
                var otherSessionMembers = GetOtherSessionMembers(session!, sessionMemberData);
                
                await _sharedFilesService.AssertUploadIsFinished(sharedFileDefinition, totalParts, 
                    otherSessionMembers.Select(sm => sm.ClientInstanceId).ToList());

                var transferPush = new FileTransferPush
                {
                    SessionId = sessionId,
                    SharedFileDefinition = sharedFileDefinition,
                    TotalParts = totalParts,
                    ActionsGroupIds = transferParameters.ActionsGroupIds
                };
                await _invokeClientsService.Clients(otherSessionMembers).UploadFinished(transferPush);
            }
            else
            {
                await _synchronizationService.OnUploadIsFinishedAsync(sharedFileDefinition, totalParts, client);
            }
        }
    }

    public async Task AssertFilePartIsUploaded(string sessionId, Client client, TransferParameters transferParameters)
    {
        var session = await _cloudSessionsRepository.Get(sessionId);
        var sessionMemberData = session?.FindMember(client.ClientInstanceId);
        var sharedFileDefinition = transferParameters.SharedFileDefinition;
        var partNumber = transferParameters.PartNumber!.Value;
        
        _ = _usageStatisticsService.RegisterUploadUsage(client, sharedFileDefinition, partNumber);

        if (sessionMemberData != null && IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition))
        {
            if (sharedFileDefinition.IsInventory || sharedFileDefinition.IsSynchronizationStartData || sharedFileDefinition.IsProfileDetails)
            {
                var otherSessionMembers = GetOtherSessionMembers(session!, sessionMemberData);
                
                await _sharedFilesService.AssertFilePartIsUploaded(sharedFileDefinition, partNumber, 
                    otherSessionMembers.Select(sm => sm.ClientInstanceId).ToList());

                var transferPush = new FileTransferPush
                {
                    SessionId = sessionId,
                    SharedFileDefinition = sharedFileDefinition,
                    PartNumber = partNumber,
                    ActionsGroupIds = transferParameters.ActionsGroupIds
                };
                await _invokeClientsService.Clients(otherSessionMembers).FilePartUploaded(transferPush);
            }
            else
            {
                await _synchronizationService.OnFilePartIsUploadedAsync(sharedFileDefinition, partNumber);
            }
        }
    }

    public async Task AssertFilePartIsDownloaded(string sessionId, Client client, SharedFileDefinition sharedFileDefinition,
        int partNumber)
    {
        var sessionMemberData = await _cloudSessionsRepository.GetSessionMember(sessionId, client);

        if (IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition))
        {
            await _sharedFilesService.AssertFilePartIsDownloaded(sharedFileDefinition, client, partNumber);
        }
    }

    public async Task AssertDownloadIsFinished(string sessionId, Client client, SharedFileDefinition sharedFileDefinition)
    {
        var sessionMemberData = await _cloudSessionsRepository.GetSessionMember(sessionId, client);

        if (IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition))
        {
            _logger.LogInformation("AssertDownloadIsFinished: {cloudSession} {sharedFileDefinition}",
                sessionMemberData!.CloudSessionData.SessionId,
                sharedFileDefinition.Id);

            if (sharedFileDefinition.IsSynchronization)
            {
                await _synchronizationService.OnDownloadIsFinishedAsync(sharedFileDefinition, client);
            }
        }
    }
    
    private bool IsSharedFileDefinitionAllowed(SessionMemberData? sessionMemberData, SharedFileDefinition? sharedFileDefinition)
    {
        bool canGetUrl = false;

        if (sessionMemberData != null && sharedFileDefinition != null)
        {
            canGetUrl = sessionMemberData.CloudSessionData.SessionMembers.Any(smd =>
                Equals(smd.ClientInstanceId, sharedFileDefinition.ClientInstanceId));
        }

        return canGetUrl;
    }
    
    private static List<SessionMemberData> GetOtherSessionMembers(CloudSessionData session, SessionMemberData sessionMemberData)
    {
        var otherSessionMembers = session.SessionMembers
            .Where(sm => !Equals(sm.ClientInstanceId, sessionMemberData.ClientInstanceId))
            .ToList();
        
        return otherSessionMembers;
    }
    
}