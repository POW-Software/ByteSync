using System.Reactive.Subjects;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Communications.SignalR;

namespace ByteSync.Client.IntegrationTests.TestHelpers.Server;

public class FakeHubPushHandler : IHubPushHandler2
{
    public Subject<(CloudSessionResult, ValidateJoinCloudSessionParameters)> YouJoinedSession { get; } = new();
    
    public Subject<string> YouGaveAWrongPassword { get; } = new();
    
    public Subject<SessionMemberInfoDTO> MemberJoinedSession { get; } = new();
    
    public Subject<SessionMemberInfoDTO> MemberQuittedSession { get; } = new();
    
    public Subject<SessionSettingsUpdatedDTO> SessionSettingsUpdated { get; } = new();
    
    public Subject<InventoryStartedDTO> InventoryStarted { get; } = new();
    
    public Subject<DataNodeDTO> DataNodeAdded { get; } = new();
    
    public Subject<DataNodeDTO> DataNodeRemoved { get; } = new();
    
    public Subject<DataSourceDTO> DataSourceAdded { get; } = new();
    
    public Subject<DataSourceDTO> DataSourceRemoved { get; } = new();
    
    public Subject<FileTransferPush> FilePartUploaded { get; } = new();
    
    public Subject<FileTransferPush> UploadFinished { get; } = new();
    
    public Subject<string> OnReconnected { get; } = new();
    
    public Subject<Synchronization> SynchronizationStarted { get; } = new();
    
    public Subject<Synchronization> SynchronizationUpdated { get; } = new();
    
    public Subject<UpdateSessionMemberGeneralStatusParameters> SessionMemberGeneralStatusUpdated { get; } = new();
    
    public Subject<SynchronizationProgressPush> SynchronizationProgressUpdated { get; } = new();
    
    public Subject<(string, string, PublicKeyInfo)> AskPublicKeyCheckData { get; } = new();
    
    public Subject<(string, PublicKeyCheckData)> GiveMemberPublicKeyCheckData { get; } = new();
    
    public Subject<RequestTrustProcessParameters> RequestTrustPublicKey { get; } = new();
    
    public Subject<DigitalSignatureCheckInfo> RequestCheckDigitalSignature { get; } = new();
    
    public Subject<PublicKeyValidationParameters> InformPublicKeyValidationIsFinished { get; } = new();
    
    public Subject<AskCloudSessionPasswordExchangeKeyPush> AskCloudSessionPasswordExchangeKey { get; } = new();
    
    public Subject<GiveCloudSessionPasswordExchangeKeyParameters> GiveCloudSessionPasswordExchangeKey { get; } = new();
    
    public Subject<AskJoinCloudSessionParameters> CheckCloudSessionPasswordExchangeKey { get; } = new();
    
    public Subject<BaseSessionDto> SessionResetted { get; } = new();
    
    public Subject<(string, LobbyMemberInfo)> MemberJoinedLobby { get; } = new();
    
    public Subject<(string, string)> MemberQuittedLobby { get; } = new();
    
    public Subject<(string, LobbyCheckInfo)> LobbyCheckInfosSent { get; } = new();
    
    public Subject<(string, string, LobbyMemberStatuses)> LobbyMemberStatusUpdated { get; } = new();
    
    public Subject<LobbyCloudSessionCredentials> LobbyCloudSessionCredentialsSent { get; } = new();
    
    public Subject<InformProtocolVersionIncompatibleParameters> InformProtocolVersionIncompatible { get; } = new();
}
