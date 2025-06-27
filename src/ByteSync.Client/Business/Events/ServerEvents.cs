using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Sessions.Cloud;
using Prism.Events;

namespace ByteSync.Business.Events;
// internal class OnServerMemberJoinedSession : PubSubEvent<CloudSessionResult> { }

// internal class OnServerYouJoinedSession : PubSubEvent<(CloudSessionResult, ValidateJoinCloudSessionParameters)> { }

// internal class OnServerYouGaveAWrongPassword : PubSubEvent<string> { }

// internal class OnServerMemberQuittedSession : PubSubEvent<CloudSessionResult> { }

// internal class OnServerSessionSettingsUpdated : PubSubEvent<(string, string, EncryptedSessionSettings)> { }

// internal class OnServerSessionResetted : PubSubEvent<string> { }

internal class OnServerSessionOnFatalError : PubSubEvent<CloudSessionFatalError> { }
    
// internal class OnServerStartInventory : PubSubEvent<(string, string, EncryptedSessionSettings)> { }
    
// internal class OnServerDataSourceAdded : PubSubEvent<(string, string, EncryptedDataSource)> { }
//
// internal class OnServerDataSourceRemoved : PubSubEvent<(string, string, EncryptedDataSource)> { }

// internal class OnServerAskPublicKeyCheckData : PubSubEvent<(string sessionId, string clientInstanceId, PublicKeyInfo publicKeyInfo)> { }
//
// internal class OnServerGiveMemberPublicKeyCheckData : PubSubEvent<(string sessionId, PublicKeyCheckData)> { }
//
// internal class OnServerRequestTrustPublicKey : PubSubEvent<RequestTrustProcessParameters> { }
    
// internal class OnServerRequestCheckDigitalSignature : PubSubEvent<DigitalSignatureCheckInfo> { }
    
// internal class OnServerInformPublicKeyValidationIsFinished : PubSubEvent<PublicKeyValidationParameters> { }
    
// internal class OnServerAskCloudSessionPasswordExchangeKey : PubSubEvent<(string sessionId, PublicKeyInfo publicKeyInfo, string clientInstanceId)> { }

// internal class OnServerGiveCloudSessionPasswordExchangeKey : PubSubEvent<GiveCloudSessionPasswordExchangeKeyParameters> { }
    
// internal class OnServerCheckCloudSessionPasswordExchangeKey : PubSubEvent<AskJoinCloudSessionParameters> { }

// internal class OnServerFilePartUploaded : PubSubEvent<(string, SharedFileDefinition, int)> { }
//
// internal class OnServerUploadFinished : PubSubEvent<(string, SharedFileDefinition, int)> { }

// internal class OnServerSynchronizationStarted : PubSubEvent<(string, string)> { }
    
// internal class OnServerSynchronizationAbortRequested : PubSubEvent<SynchronizationAbortRequest> { }
    
// internal class OnServerSynchronizationEnded : PubSubEvent<SynchronizationEnd> { }
    
// internal class OnServerReconnected : PubSubEvent<string> { }

// internal class OnServerLocalInventoryStatusChanged : PubSubEvent<UpdateSessionMemberGeneralStatusParameters> { }
//
// internal class OnServerSynchronizationProgressChanged : PubSubEvent<string> { }
    
internal class OnServerMemberJoinedLobby : PubSubEvent<(string, LobbyMemberInfo)> { }
    
internal class OnServerMemberQuittedLobby : PubSubEvent<(string, string)> { }
    
internal class OnServerLobbyCheckInfosSent : PubSubEvent<(string, LobbyCheckInfo)> { }
    
internal class OnServerLobbyMemberStatusUpdated : PubSubEvent<(string, string, LobbyMemberStatuses)> { }
    
internal class OnServerLobbyCloudSessionCredentialsSent : PubSubEvent<LobbyCloudSessionCredentials> { }