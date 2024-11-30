// using System.Collections.Generic;
// using System.Runtime.CompilerServices;
// using System.Threading.Tasks;
// using ByteSync.Common.Business.Actions;
// using ByteSync.Common.Business.Misc;
// using ByteSync.Common.Business.Sessions.Cloud;
// using ByteSync.Common.Business.SharedFiles;
// using ByteSync.Common.Business.Synchronizations;
// using ByteSync.Common.Business.Synchronizations.Light;
// using ByteSync.Interfaces;
// using ByteSync.Interfaces.Controls.Communications.Http;
// using RestSharp;
// using Serilog;
//
// namespace ByteSync.Services.Communications.Api;
//
// public class ApiConnectionWrapper : IApiByteSyncInvoke
// {
//     private readonly IApiInvoker _apiInvoker;
//
//     public ApiConnectionWrapper(IApiInvoker? apiInvoker = null)
//     {
//         _apiInvoker = apiInvoker!;
//     }
//     
//     public async Task<Synchronization?> GetSynchronizationStart(string sessionId)
//     {
//         try
//         {
//             var synchronizationStart = await _apiInvoker.InvokeRestAsync<Synchronization?>(
//                 Method.Get, 
//                 $"/session/{sessionId}/synchronizationStart", 
//                 null, null);
//
//             return synchronizationStart;
//
//             //return await Invoker.Invoke<SynchronizationStart>("session/getSynchronizationStart", "sessionId", sessionId);
//         }
//         catch (Exception ex)
//         {
//             LogError(ex);
//                 
//             throw;
//         }
//     }
//
//     public async Task<Synchronization?> StartSynchronization(CloudSession cloudSession, List<ActionsGroupDefinition> synchronizationActionDefinitions)
//     {
//         try
//         {
//             var synchronizationStart = await _apiInvoker.InvokeRestAsync<Synchronization?>(
//                 Method.Post, 
//                 $"/session/{cloudSession.SessionId}/startSynchronization", 
//                 null, synchronizationActionDefinitions);
//             
//             return synchronizationStart;
//         }
//         catch (Exception ex)
//         {
//             LogError(ex);
//                 
//             throw;
//         }
//     }
//
//     public async Task InformSynchronizationIsFinished(CloudSession cloudSession)
//     {
//         try
//         {
//             var synchronizationStart = await _apiInvoker.InvokeRestAsync<Synchronization>(
//                 Method.Post, 
//                 $"/session/{cloudSession.SessionId}/informSynchronizationIsFinished", 
//                 null, null);
//         }
//         catch (Exception ex)
//         {
//             LogError(ex);
//                 
//             throw;
//         }
//     }
//     
//     public async Task<UsageStatisticsData> GetUsageStatistics(UsageStatisticsRequest usageStatisticsRequest)
//     {
//         try
//         {
//             var usageStatisticsData = await _apiInvoker.InvokeRestAsync<UsageStatisticsData>(
//                 Method.Post, 
//                 $"/usageStatistics", 
//                 null, usageStatisticsRequest);
//
//             return usageStatisticsData;
//         }
//         catch (Exception ex)
//         {
//             LogError(ex);
//                 
//             throw;
//         }
//     }
//     
//     // public async Task<List<string>> GetActionsGroupsIds(string sessionId, SharedFileDefinition sharedFileDefinition)
//     // {
//     //     try
//     //     {
//     //         var actionsGroupsIds = await _apiInvoker.InvokeRestAsync<List<string>>(
//     //             Method.Get, 
//     //             $"/session/{sessionId}/sharedFileDefinition/{sharedFileDefinition.Id}/actionsGroupsIds", 
//     //             null, null);
//     //
//     //         return actionsGroupsIds!;
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //             
//     //         throw;
//     //     }
//     // }
//
//     public async Task<List<SynchronizationProgressInfo>?> GetSynchronizationProgressInfos(string? sessionId, string synchronizationProgressInfosId)
//     {
//         try
//         {
//             var synchronizationProgressInfos = await _apiInvoker.InvokeRestAsync<List<SynchronizationProgressInfo>>(
//                 Method.Get, 
//                 $"/session/{sessionId}/synchronizationProgressInfos/{synchronizationProgressInfosId}", 
//                 null, null);
//
//             return synchronizationProgressInfos!;
//         }
//         catch (Exception ex)
//         {
//             LogError(ex);
//                 
//             throw;
//         }
//     }
//
//
//     // public async Task SetActionsGroupsIds(string sessionId, SharedFileDefinition sharedFileDefinition, List<string> actionGroupsIds)
//     // {
//     //     try
//     //     {
//     //         await _apiInvoker.InvokeRestAsync<List<string>>(
//     //             Method.Post, 
//     //             $"/session/{sessionId}/sharedFileDefinition/{sharedFileDefinition.Id}/actionsGroupsIds", 
//     //             null, actionGroupsIds);
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         LogError(ex);
//     //             
//     //         throw;
//     //     }
//     // }
//     
//     private void LogError(Exception exception, [CallerMemberName] string caller = "")
//     {
//         // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
//         Log.Error(exception, caller);
//     }
// }