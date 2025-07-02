using System.Threading.Tasks;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;

namespace ByteSync.Common.Interfaces.Hub;

public interface IHubByteSyncPush
{
    Task YouJoinedSession(CloudSessionResult cloudSessionResult, ValidateJoinCloudSessionParameters parameters);

    Task YouGaveAWrongPassword(string sessionId);

    Task AskPublicKeyCheckData(string sessionId, string clientInstanceId, PublicKeyInfo publicKeyInfo);

    Task GiveMemberPublicKeyCheckData(string sessionId, PublicKeyCheckData memberPublicKeyCheckData);

    Task AskCloudSessionPasswordExchangeKey(AskCloudSessionPasswordExchangeKeyPush askCloudSessionPasswordExchangeKeyPush);

    Task CheckCloudSessionPasswordExchangeKey(AskJoinCloudSessionParameters parameters);

    Task GiveCloudSessionPasswordExchangeKey(GiveCloudSessionPasswordExchangeKeyParameters parameters);

    Task MemberJoinedSession(SessionMemberInfoDTO sessionMemberInfo);

    Task MemberQuittedSession(SessionMemberInfoDTO sessionMemberInfo);

    Task OnReconnected(string sessionId);

    Task SessionSettingsUpdated(SessionSettingsUpdatedDTO sessionSettingsUpdatedDto);

    // Task SessionOnFatalError(CloudSessionFatalError cloudSessionFatalError);

    Task InventoryStarted(InventoryStartedDTO inventoryStartedDto);

    Task DataNodeAdded(DataNodeDTO dataNodeDto);

    Task DataNodeRemoved(DataNodeDTO dataNodeDto);

    Task DataSourceAdded(DataSourceDTO dataSourceDto);

    Task DataSourceRemoved(DataSourceDTO dataSourceDto);

    Task FilePartUploaded(FileTransferPush fileTransferPush);

    Task UploadFinished(FileTransferPush fileTransferPush);

    Task SessionMemberGeneralStatusUpdated(UpdateSessionMemberGeneralStatusParameters updateSessionMemberGeneralStatusParameters);

    Task SynchronizationProgressUpdated(SynchronizationProgressPush synchronizationProgressPush);

    Task SynchronizationStarted(Synchronization synchronization);
    
    Task SynchronizationUpdated(Synchronization synchronization);

    Task SessionResetted(BaseSessionDto baseSessionDto);

    Task MemberJoinedLobby(string lobbyId, LobbyMemberInfo lobbyMemberInfo);

    Task MemberQuittedLobby(string lobbyId, string clientInstanceId);

    Task LobbyCheckInfosSent(string lobbyId, LobbyCheckInfo lobbyCheckInfo);

    Task LobbyMemberStatusUpdated(string lobbyId, string clientInstanceId, LobbyMemberStatuses lobbyMemberStatus);

    Task LobbyCloudSessionCredentialsSent(LobbyCloudSessionCredentials credentials);

    Task RequestTrustPublicKey(RequestTrustProcessParameters parameters);

    Task InformPublicKeyValidationIsFinished(PublicKeyValidationParameters parameters);

    Task RequestCheckDigitalSignature(DigitalSignatureCheckInfo digitalSignatureCheckInfo);
}