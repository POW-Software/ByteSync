using System.Reactive.Subjects;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;

namespace ByteSync.Interfaces.Controls.Communications.SignalR;

public interface IHubPushHandler2
{
    Subject<(CloudSessionResult, ValidateJoinCloudSessionParameters)> YouJoinedSession { get; }
    Subject<string> YouGaveAWrongPassword { get; }
    Subject<SessionMemberInfoDTO> MemberJoinedSession { get; }
    Subject<SessionMemberInfoDTO> MemberQuittedSession { get; }
    Subject<SessionSettingsUpdatedDTO> SessionSettingsUpdated { get; }
    Subject<InventoryStartedDTO> InventoryStarted { get; }
    Subject<DataNodeDTO> DataNodeAdded { get; }
    Subject<DataNodeDTO> DataNodeRemoved { get; }
    Subject<DataSourceDTO> DataSourceAdded { get; }
    Subject<DataSourceDTO> DataSourceRemoved { get; }
    Subject<FileTransferPush> FilePartUploaded { get; }
    Subject<FileTransferPush> UploadFinished { get; }
    Subject<string> OnReconnected { get; }
    Subject<Synchronization> SynchronizationStarted { get; }
    Subject<Synchronization> SynchronizationUpdated { get; }
    Subject<UpdateSessionMemberGeneralStatusParameters> SessionMemberGeneralStatusUpdated { get; }
    Subject<SynchronizationProgressPush> SynchronizationProgressUpdated { get; }
    Subject<(string, string, PublicKeyInfo)> AskPublicKeyCheckData { get; }
    Subject<(string, PublicKeyCheckData)> GiveMemberPublicKeyCheckData { get; }
    Subject<RequestTrustProcessParameters> RequestTrustPublicKey { get; }
    Subject<DigitalSignatureCheckInfo> RequestCheckDigitalSignature { get; }
    Subject<PublicKeyValidationParameters> InformPublicKeyValidationIsFinished { get; }
    Subject<AskCloudSessionPasswordExchangeKeyPush> AskCloudSessionPasswordExchangeKey { get; }
    Subject<GiveCloudSessionPasswordExchangeKeyParameters> GiveCloudSessionPasswordExchangeKey { get; }
    Subject<AskJoinCloudSessionParameters> CheckCloudSessionPasswordExchangeKey { get; }
    Subject<BaseSessionDto> SessionResetted { get; }
    Subject<(string, LobbyMemberInfo)> MemberJoinedLobby { get; }
    Subject<(string, string)> MemberQuittedLobby { get; }
    Subject<(string, LobbyCheckInfo)> LobbyCheckInfosSent { get; }
    Subject<(string, string, LobbyMemberStatuses)> LobbyMemberStatusUpdated { get; }
    Subject<LobbyCloudSessionCredentials> LobbyCloudSessionCredentialsSent { get; }
}