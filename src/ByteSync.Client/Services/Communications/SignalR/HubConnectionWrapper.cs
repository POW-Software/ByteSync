// using System.Collections.Generic;
// using System.Reactive.Linq;
// using System.Runtime.CompilerServices;
// using System.Threading.Tasks;
// using ByteSync.Common.Business.EndPoints;
// using ByteSync.Common.Business.Inventories;
// using ByteSync.Common.Business.Lobbies;
// using ByteSync.Common.Business.Lobbies.Connections;
// using ByteSync.Common.Business.Profiles;
// using ByteSync.Common.Business.Sessions;
// using ByteSync.Common.Business.Sessions.Cloud;
// using ByteSync.Common.Business.Sessions.Cloud.Connections;
// using ByteSync.Common.Business.SharedFiles;
// using ByteSync.Common.Business.Synchronizations;
// using ByteSync.Common.Interfaces.Hub;
// using ByteSync.Interfaces.Controls.Communications.SignalR;
// using ByteSync.Interfaces.Services.Communications;
// using Microsoft.AspNetCore.SignalR.Client;
// using ReactiveUI;
// using Serilog;
//
// namespace ByteSync.Services.Communications.SignalR;
//
// /// <summary>
// /// https://medium.com/accurx-techblog/type-safety-with-c-clients-and-signalr-dcde5da20624
// /// </summary>
// class HubConnectionWrapper : IHubByteSyncInvoke
// {
//     private readonly IHubInvoker _hubInvoker;
//     
//     public HubConnectionWrapper(IHubInvoker? hubInvoker = null)
//     {
//         _hubInvoker = hubInvoker!;
//         // Invoker = new Invoker();
//         
//         // connectionService.Connection.Where(c => c != null)
//         //     .Subscribe(c => Invoker = new HubInvoker(connection))
//         //
//         // ;
//     }
//
//
//     // public async Task<CloudSessionResult> CreateCloudSession(CreateCloudSessionParameters parameters)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<CloudSessionResult>(nameof(IHubByteSyncInvoke.CreateCloudSession),
//     //             parameters);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task<ClientsFullIdsCollection> GetCloudSessionMembersAndStartTrustCheck(GetCloudSessionMembersParameters parameters)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<ClientsFullIdsCollection>(nameof(IHubByteSyncInvoke.GetCloudSessionMembersAndStartTrustCheck),
//     //             parameters);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task SendDigitalSignatures(string sessionId, List<DigitalSignatureCheckInfo> digitalSignatureCheckInfos, bool isCheckOK)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.SendDigitalSignatures),
//     //             sessionId, digitalSignatureCheckInfos, isCheckOK);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task SetAuthChecked(string sessionId, string checkedClientInstanceId)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.SetAuthChecked),
//     //             sessionId, checkedClientInstanceId);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task RequestTrustPublicKey(RequestTrustProcessParameters parameters)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.RequestTrustPublicKey),
//     //             parameters);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task GiveMemberPublicKeyCheckData(string sessionId, PublicKeyCheckData memberPublicKeyCheckData, string clientInstanceId)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.GiveMemberPublicKeyCheckData),
//     //             sessionId, memberPublicKeyCheckData, clientInstanceId);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task InformPublicKeyValidationIsFinished(PublicKeyValidationParameters parameters)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.InformPublicKeyValidationIsFinished),
//     //             parameters);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//
//     // public async Task<List<PublicKeyInfo>> AskCloudSessionMembersPublicKeys(AskCloudSessionPasswordExchangeKeyParameters parameters)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<List<PublicKeyInfo>>(nameof(IHubByteSyncInvoke.AskCloudSessionMembersPublicKeys),
//     //             parameters);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//
//     // public async Task SetCloudSessionTrustedPublicKeys(AskCloudSessionPasswordExchangeKeyParameters parameters, 
//     //     List<PublicKeyCheckData> publicKeyCheckDatas)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke<List<PublicKeyInfo>>(nameof(IHubByteSyncInvoke.SetCloudSessionTrustedPublicKeys),
//     //             parameters, publicKeyCheckDatas);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//
//     // public async Task InformValidatorIsNotTrusted(GiveCloudSessionPasswordExchangeKeyParameters parameters, PublicKeyCheckData joinerPublicKeyCheckData)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.InformValidatorIsNotTrusted),
//     //             parameters, joinerPublicKeyCheckData);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//
//     // public async Task<JoinSessionResult> AskJoinCloudSession(AskJoinCloudSessionParameters parameters)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<JoinSessionResult>(nameof(IHubByteSyncInvoke.AskJoinCloudSession),
//     //             parameters);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task<JoinSessionResult> AskCloudSessionPasswordExchangeKey(AskCloudSessionPasswordExchangeKeyParameters parameters)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<JoinSessionResult>(nameof(IHubByteSyncInvoke.AskCloudSessionPasswordExchangeKey),
//     //             parameters);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task GiveCloudSessionPasswordExchangeKey(GiveCloudSessionPasswordExchangeKeyParameters parameters)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.GiveCloudSessionPasswordExchangeKey), 
//     //             parameters);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //             
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task ValidateJoinCloudSession(ValidateJoinCloudSessionParameters parameters)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.ValidateJoinCloudSession), parameters);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task<FinalizeJoinSessionResult> FinalizeJoinCloudSession(FinalizeJoinCloudSessionParameters parameters)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<FinalizeJoinSessionResult>(nameof(IHubByteSyncInvoke.FinalizeJoinCloudSession), parameters);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task InformPasswordIsWrong(string sessionId, string clientInstanceId)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.InformPasswordIsWrong), 
//     //             sessionId, clientInstanceId);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task QuitCloudSession(string sessionId)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.QuitCloudSession), sessionId);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     
//     // public async Task<CloudSessionDetails> GetCloudSessionDetails(string cloudSessionId)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<CloudSessionDetails>(nameof(IHubByteSyncInvoke.GetCloudSessionDetails),
//     //             cloudSessionId);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//
//     // public async Task<List<SessionMemberInfo>> GetSessionMembers(string sessionId)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<List<SessionMemberInfo>>(nameof(IHubByteSyncInvoke.GetSessionMembers), 
//     //             sessionId);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//
//     // public async Task<bool> SetPathItemAdded(string sessionId, EncryptedPathItem encryptedPathItem)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<bool>(nameof(IHubByteSyncInvoke.SetPathItemAdded), 
//     //             sessionId, encryptedPathItem);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     // public async Task<bool> SetPathItemRemoved(string sessionId, EncryptedPathItem encryptedPathItem)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<bool>(nameof(IHubByteSyncInvoke.SetPathItemRemoved), sessionId, encryptedPathItem);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//
//     // public async Task<List<EncryptedPathItem>> GetPathItems(string sessionId, string clientInstanceId)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<List<EncryptedPathItem>>(nameof(IHubByteSyncInvoke.GetPathItems), sessionId, clientInstanceId); 
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//
//     // public async Task<string> GetUploadFileUrl(string sessionId, SharedFileDefinition sharedFileDefinition, int partNumber)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<string>(nameof(IHubByteSyncInvoke.GetUploadFileUrl), 
//     //             sessionId, sharedFileDefinition, partNumber);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task<string> GetDownloadFileUrl(string sessionId, SharedFileDefinition sharedFileDefinition, int partNumber)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<string>(nameof(IHubByteSyncInvoke.GetDownloadFileUrl), 
//     //             sessionId, sharedFileDefinition, partNumber);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task AssertFilePartIsUploaded(string sessionId, SharedFileDefinition sharedFileDefinition, int partNumber)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.AssertFilePartIsUploaded),
//     //             sessionId, sharedFileDefinition, partNumber);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task AssertUploadIsFinished(string sessionId, SharedFileDefinition sharedFileDefinition, int totalParts)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.AssertUploadIsFinished),
//     //             sessionId, sharedFileDefinition, totalParts);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //     
//     // public async Task AssertFilePartIsDownloaded(string sessionId, SharedFileDefinition sharedFileDefinition, int partNumber)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.AssertFilePartIsDownloaded),
//     //             sessionId, sharedFileDefinition, partNumber);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task AssertDownloadIsFinished(string sessionId, SharedFileDefinition sharedFileDefinition)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.AssertDownloadIsFinished),
//     //             sessionId, sharedFileDefinition);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//
//     // public async Task AssertLocalInventoryStatusChanged(SetLocalInventoryStatusParameters setLocalInventoryStatusParameters)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.AssertLocalInventoryStatusChanged), 
//     //             setLocalInventoryStatusParameters);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//
//     // public async Task AssertDateIsCopied(string sessionId, List<string> actionsGroupIds)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.AssertDateIsCopied), 
//     //             sessionId, actionsGroupIds);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //     
//     // public async Task AssertFileOrDirectoryIsDeleted(string sessionId, List<string> actionsGroupIds)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.AssertFileOrDirectoryIsDeleted), 
//     //             sessionId, actionsGroupIds);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //     
//     // public async Task AssertDirectoryIsCreated(string sessionId, List<string> actionsGroupIds)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.AssertDirectoryIsCreated), 
//     //             sessionId, actionsGroupIds);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //     
//     // public async Task AssertLocalCopyIsDone(string sessionId, List<string> actionsGroupIds)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.AssertLocalCopyIsDone), 
//     //             sessionId, actionsGroupIds);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task AssertSynchronizationActionError(string sessionId, SharedFileDefinition sharedFileDefinition)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.AssertSynchronizationActionError), 
//     //             sessionId, sharedFileDefinition);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//     //
//     // public async Task AssertSynchronizationActionErrors(string sessionId, List<string> actionsGroupIds)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.AssertSynchronizationActionErrors), 
//     //             sessionId, actionsGroupIds);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//
//     // public async Task SendUpdatedSessionSettings(string sessionId, EncryptedSessionSettings sessionSettings)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.SendUpdatedSessionSettings), 
//     //             sessionId, sessionSettings);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//
//     // public async Task<StartInventoryResult> StartInventory(string sessionId, EncryptedSessionSettings sessionSettings)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<StartInventoryResult>(nameof(IHubByteSyncInvoke.StartInventory), 
//     //             sessionId, sessionSettings);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//
//     // public async Task ResetSession(string sessionId)
//     // {
//     //     try
//     //     {
//     //         await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.ResetSession), 
//     //             sessionId);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//
//     // public async Task<SynchronizationAbortRequest?> RequestAbortSynchronization(string sessionId)
//     // {
//     //     try
//     //     {
//     //         return await _hubInvoker.Invoke<SynchronizationAbortRequest>(nameof(IHubByteSyncInvoke.RequestAbortSynchronization), 
//     //             sessionId);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //
//     //         throw;
//     //     }
//     // }
//         
//     public async Task<JoinLobbyResult> JoinLobby(JoinLobbyParameters joinLobbyParameters)
//     {
//         try
//         {
//             return await _hubInvoker.Invoke<JoinLobbyResult>(nameof(IHubByteSyncInvoke.JoinLobby), 
//                 joinLobbyParameters);
//         }
//         catch (Exception ex)
//         {
//             LogError(ex);
//
//             throw;
//         }
//     }
//
//     public async Task<string?> GetProfileDetailsPassword(GetProfileDetailsPasswordParameters parameters)
//     {
//         try
//         {
//             return await _hubInvoker.Invoke<string?>(nameof(IHubByteSyncInvoke.GetProfileDetailsPassword), 
//                 parameters);
//         }
//         catch (Exception ex)
//         {
//             LogError(ex);
//
//             throw;
//         }
//     }
//
//     public async Task SendLobbyCheckInfos(LobbyCheckInfo lobbyCheckInfo)
//     {
//         try
//         {
//             await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.SendLobbyCheckInfos), 
//                 lobbyCheckInfo);
//         }
//         catch (Exception ex)
//         {
//             LogError(ex);
//
//             throw;
//         }
//     }
//
//     public async Task UpdateLobbyMemberStatus(string lobbyId, LobbyMemberStatuses lobbyMemberStatus)
//     {
//         try
//         {
//             await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.UpdateLobbyMemberStatus), 
//                 lobbyId, lobbyMemberStatus);
//         }
//         catch (Exception ex)
//         {
//             LogError(ex);
//
//             throw;
//         }
//     }
//
//     public async Task<CreateCloudSessionProfileResult> CreateCloudSessionProfile(string sessionId)
//     {
//         try
//         {
//             return await _hubInvoker.Invoke<CreateCloudSessionProfileResult>(nameof(IHubByteSyncInvoke.CreateCloudSessionProfile), 
//                 sessionId);
//         }
//         catch (Exception ex)
//         {
//             LogError(ex);
//
//             throw;
//         }
//     }
//
//     public async Task<bool> DeleteCloudSessionProfile(DeleteCloudSessionProfileParameters parameters)
//     {
//         try
//         {
//             return await _hubInvoker.Invoke<bool>(nameof(IHubByteSyncInvoke.DeleteCloudSessionProfile), 
//                 parameters);
//         }
//         catch (Exception ex)
//         {
//             LogError(ex);
//
//             throw;
//         }
//     }
//
//     public async Task<CloudSessionProfileData> GetCloudSessionProfileData(string sessionId, string cloudSessionProfileId)
//     {
//         try
//         {
//             return await _hubInvoker.Invoke<CloudSessionProfileData>(nameof(IHubByteSyncInvoke.GetCloudSessionProfileData), 
//                 sessionId, cloudSessionProfileId);
//         }
//         catch (Exception ex)
//         {
//             LogError(ex);
//
//             throw;
//         }
//     }
//
//     public async Task SendLobbyCloudSessionCredentials(LobbyCloudSessionCredentials credentials)
//     {
//         try
//         {
//             await _hubInvoker.Invoke(nameof(IHubByteSyncInvoke.SendLobbyCloudSessionCredentials), 
//                 credentials);
//         }
//         catch (Exception ex)
//         {
//             LogError(ex);
//
//             throw;
//         }
//     }
//
//     public async Task<bool> QuitLobby(string lobbyId)
//     {
//         try
//         {
//             return await _hubInvoker.Invoke<bool>(nameof(IHubByteSyncInvoke.QuitLobby), 
//                 lobbyId);
//         }
//         catch (Exception ex)
//         {
//             LogError(ex);
//
//             throw;
//         }
//     }
//
//     private void LogError(Exception exception, [CallerMemberName] string caller = "")
//     {
//         // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
//         Log.Error(exception, caller);
//     }
// }