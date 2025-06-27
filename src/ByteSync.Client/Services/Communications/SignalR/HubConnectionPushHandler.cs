using System.Runtime.CompilerServices;
using ByteSync.Business.Events;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Prism.Events;
using Serilog;

namespace ByteSync.Services.Communications.SignalR;

class HubConnectionPushHandler
{
    private readonly IEventAggregator _eventAggregator;

    internal HubConnectionPushHandler(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }

    public void HandleConnection(HubConnection connection)
    {
        // connection.BindOnInterface<string>(push => push.Ping, OnPing);
        // connection.BindOnInterface<CloudSessionResult, ValidateJoinCloudSessionParameters>(push => push.YouJoinedSession, OnYouJoinedSession);
        // connection.BindOnInterface<string>(push => push.YouGaveAWrongPassword, OnYouGaveAWrongPassword);
        // connection.BindOnInterface<CloudSessionResult>(push => push.MemberJoinedSession, OnMemberJoinedSession);
        // connection.BindOnInterface<CloudSessionResult>(push => push.MemberQuittedSession, OnMemberQuittedSession);
        // connection.BindOnInterface<SessionSettingsUpdatedDTO>(push => push.SessionSettingsUpdated, OnSessionSettingsUpdated);
        connection.BindOnInterface<CloudSessionFatalError>(push => push.SessionOnFatalError, OnSessionOnFatalError);
        // connection.BindOnInterface<string, string, EncryptedSessionSettings>(push => push.InventoryStarted, OnStartInventory);
        // connection.BindOnInterface<string, string, EncryptedDataSource>(push => push.DataSourceAdded, OnDataSourceAdded);
        // connection.BindOnInterface<string, string, EncryptedDataSource>(push => push.DataSourceRemoved, OnDataSourceRemoved);
        // connection.BindOnInterface<string, SharedFileDefinition, int>(push => push.FilePartUploaded, OnFilePartUploaded);
        // connection.BindOnInterface<string, SharedFileDefinition, int>(push => push.UploadFinished, OnUploadFinished);
        // connection.BindOnInterface<string>(push => push.OnReconnected, OnReconnected);
        // connection.BindOnInterface<string, string>(push => push.SynchronizationStarted, OnSynchronizationStarted);
        // connection.BindOnInterface<SynchronizationAbortRequest>(push => push.SynchronizationAbortRequested, OnSynchronizationAbortRequested);
        // connection.BindOnInterface<SynchronizationEnd>(push => push.SynchronizationEnded, OnSynchronizationEnded);
        // connection.BindOnInterface<UpdateSessionMemberGeneralStatusParameters>(push => push.SessionMemberGeneralStatusUpdated, OnLocalInventoryStatusChanged);
        // connection.BindOnInterface<string>(push => push.SynchronizationProgressChanged, OnSynchronizationProgressChanged);
        // connection.BindOnInterface<string, string, PublicKeyInfo>(push => push.AskPublicKeyCheckData, OnAskPublicKeyCheckData);
        // connection.BindOnInterface<string, PublicKeyCheckData>(push => push.GiveMemberPublicKeyCheckData, OnGiveMemberPublicKeyCheckData);
        // connection.BindOnInterface<RequestTrustProcessParameters>(push => push.RequestTrustPublicKey, OnRequestTrustPublicKey);
        // connection.BindOnInterface<DigitalSignatureCheckInfo>(push => push.RequestCheckDigitalSignature, OnRequestCheckDigitalSignature);
        // connection.BindOnInterface<PublicKeyValidationParameters>(push => push.InformPublicKeyValidationIsFinished, OnInformPublicKeyValidationIsFinished);
        // connection.BindOnInterface<string, PublicKeyInfo, string>(push => push.AskCloudSessionPasswordExchangeKey, OnAskCloudSessionPasswordExchangeKey);
        // connection.BindOnInterface<GiveCloudSessionPasswordExchangeKeyParameters>(push => push.GiveCloudSessionPasswordExchangeKey, OnGiveCloudSessionPasswordExchangeKey);
        // connection.BindOnInterface<AskJoinCloudSessionParameters>(push => push.CheckCloudSessionPasswordExchangeKey, OnCheckCloudSessionPasswordExchangeKey);
        // connection.BindOnInterface<string, string>(push => push.SessionResetted, OnSessionResetted);
        connection.BindOnInterface<string, LobbyMemberInfo>(push => push.MemberJoinedLobby, OnMemberJoinedLobby);
        connection.BindOnInterface<string, string>(push => push.MemberQuittedLobby, OnMemberQuittedLobby);
        connection.BindOnInterface<string, LobbyCheckInfo>(push => push.LobbyCheckInfosSent, OnLobbyCheckInfosSent);
        connection.BindOnInterface<string, string, LobbyMemberStatuses>(push => push.LobbyMemberStatusUpdated, OnLobbyMemberStatusUpdated);
        connection.BindOnInterface<LobbyCloudSessionCredentials>(push => push.LobbyCloudSessionCredentialsSent, OnLobbyCloudSessionCredentialsSent);
    }

    private void OnPing(string ping)
    {
        LogDebug();
    }

    // private void OnYouJoinedSession(CloudSessionResult cloudSessionResult, ValidateJoinCloudSessionParameters parameters)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerYouJoinedSession>().Publish((cloudSessionResult, parameters));
    // }

    // private void OnYouGaveAWrongPassword(string sessionId)
    // {
    //     LogDebug();
    //         
    //     _eventAggregator.GetEvent<OnServerYouGaveAWrongPassword>().Publish(sessionId);
    // }
    
    // private void OnAskPublicKeyCheckData(string sessionId, string clientInstanceId, PublicKeyInfo publicKeyInfo)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerAskPublicKeyCheckData>()
    //         .Publish((sessionId, clientInstanceId, publicKeyInfo));
    // }
    
    // private void OnGiveMemberPublicKeyCheckData(string sessionId, PublicKeyCheckData memberPublicKeyCheckData)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerGiveMemberPublicKeyCheckData>()
    //         .Publish((sessionId, memberPublicKeyCheckData));
    // }
    
    // private void OnRequestTrustPublicKey(RequestTrustProcessParameters requestTrustProcessParameters)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerRequestTrustPublicKey>()
    //         .Publish(requestTrustProcessParameters);
    // }
    //
    // private void OnRequestCheckDigitalSignature(DigitalSignatureCheckInfo digitalSignatureCheckInfo)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerRequestCheckDigitalSignature>()
    //         .Publish(digitalSignatureCheckInfo);
    // }
    
    // private void OnInformPublicKeyValidationIsFinished(PublicKeyValidationParameters publicKeyValidationParameters)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerInformPublicKeyValidationIsFinished>()
    //         .Publish(publicKeyValidationParameters);
    // }

    // private void OnAskCloudSessionPasswordExchangeKey(string sessionId, PublicKeyInfo publicKeyInfo, string clientInstanceId)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerAskCloudSessionPasswordExchangeKey>()
    //         .Publish((sessionId, publicKeyInfo, clientInstanceId));
    // }

    // private void OnGiveCloudSessionPasswordExchangeKey(GiveCloudSessionPasswordExchangeKeyParameters parameters)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerGiveCloudSessionPasswordExchangeKey>()
    //         .Publish(parameters);
    // }

    // private void OnCheckCloudSessionPasswordExchangeKey(AskJoinCloudSessionParameters parameters)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerCheckCloudSessionPasswordExchangeKey>().Publish(parameters);
    // }

    // private void OnMemberJoinedSession(CloudSessionResult cloudSessionResult)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerMemberJoinedSession>().Publish(cloudSessionResult);
    // }

    // private void OnMemberQuittedSession(CloudSessionResult cloudSessionResult)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerMemberQuittedSession>().Publish(cloudSessionResult);
    // }
        
    // private void OnSessionSettingsUpdated(string sessionId, string clientInstanceId, EncryptedSessionSettings sessionSettings)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerSessionSettingsUpdated>().Publish((sessionId, clientInstanceId, sessionSettings));
    // }
        
    // private void OnSessionResetted(string sessionId, string clientInstanceId)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerSessionResetted>().Publish(sessionId);
    // }
        
    private void OnSessionOnFatalError(CloudSessionFatalError cloudSessionFatalError)
    {
        LogDebug();

        _eventAggregator.GetEvent<OnServerSessionOnFatalError>().Publish(cloudSessionFatalError);
    }

    // private void OnStartInventory(string sessionId, string clientInstanceId, EncryptedSessionSettings sessionSettings)
    // {
    //     _eventAggregator.GetEvent<OnServerStartInventory>().Publish((sessionId, clientInstanceId, sessionSettings));
    // }

    // private void OnDataSourceAdded(string sessionId, string clientInstanceId, EncryptedDataSource sharedDataSource)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerDataSourceAdded>().Publish((sessionId, clientInstanceId, sharedDataSource));
    // }
    //
    // private void OnDataSourceRemoved(string sessionId, string clientInstanceId, EncryptedDataSource sharedDataSource)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerDataSourceRemoved>().Publish((sessionId, clientInstanceId, sharedDataSource));
    // }

    // private void OnFilePartUploaded(string sessionId, SharedFileDefinition sharedFileDefinition, int partNumber)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerFilePartUploaded>().Publish((sessionId, sharedFileDefinition, partNumber));
    // }
    //
    // private void OnUploadFinished(string sessionId, SharedFileDefinition sharedFileDefinition, int totalParts)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerUploadFinished>().Publish((sessionId, sharedFileDefinition, totalParts));
    // }

    // private void OnReconnected(string sessionId)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerReconnected>().Publish(sessionId);
    // }

    // private void OnSynchronizationStarted(string sessionId, string startedBy)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerSynchronizationStarted>().Publish((sessionId, startedBy));
    // }
        
    // private void OnSynchronizationAbortRequested(SynchronizationAbortRequest synchronizationAbortRequest)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerSynchronizationAbortRequested>().Publish(synchronizationAbortRequest);
    // }
        
    // private void OnSynchronizationEnded(SynchronizationEnd synchronizationEnd)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerSynchronizationEnded>().Publish(synchronizationEnd);
    // }

    // private void OnLocalInventoryStatusChanged(UpdateSessionMemberGeneralStatusParameters updateSessionMemberGeneralStatusParameters)
    // {
    //     LogDebug();
    //
    //     _eventAggregator.GetEvent<OnServerLocalInventoryStatusChanged>().Publish(updateSessionMemberGeneralStatusParameters);
    // }

    // private void OnSynchronizationProgressChanged(string synchronizationProgressInfosIds)
    // {
    //     LogDebug();
    //     
    //     _eventAggregator.GetEvent<OnServerSynchronizationProgressChanged>().Publish(synchronizationProgressInfosIds);
    // }
        
    private void OnMemberJoinedLobby(string lobbyId, LobbyMemberInfo lobbyMemberInfo)
    {
        LogDebug();
            
        _eventAggregator.GetEvent<OnServerMemberJoinedLobby>().Publish((lobbyId, lobbyMemberInfo));
    }
        
    private void OnMemberQuittedLobby(string lobbyId, string clientInstanceId)
    {
        LogDebug();
            
        _eventAggregator.GetEvent<OnServerMemberQuittedLobby>().Publish((lobbyId, clientInstanceId));
    }
    
    private void OnLobbyCheckInfosSent(string lobbyId, LobbyCheckInfo lobbyCheckInfo)
    {
        LogDebug();
            
        _eventAggregator.GetEvent<OnServerLobbyCheckInfosSent>().Publish((lobbyId, lobbyCheckInfo));
    }
    
    private void OnLobbyMemberStatusUpdated(string lobbyId, string clientInstanceId, LobbyMemberStatuses lobbyMemberStatus)
    {
        LogDebug();
            
        _eventAggregator.GetEvent<OnServerLobbyMemberStatusUpdated>().Publish((lobbyId, clientInstanceId, lobbyMemberStatus));
    }
    
    private void OnLobbyCloudSessionCredentialsSent(LobbyCloudSessionCredentials lobbyCloudSessionCredentials)
    {
        LogDebug();
            
        _eventAggregator.GetEvent<OnServerLobbyCloudSessionCredentialsSent>().Publish(lobbyCloudSessionCredentials);
    }

    private void LogDebug([CallerMemberName] string caller = "")
    {
        Log.Debug($"ConnectionPushHandler.{caller}");
    }
}