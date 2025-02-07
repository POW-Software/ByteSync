﻿using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Services.Sessions;
using Serilog;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationActionServerInformer : ISynchronizationActionServerInformer
{
    private readonly ISessionService _sessionDataHolder;
    private readonly ISynchronizationApiClient _synchronizationApiClient;

    public SynchronizationActionServerInformer(ISessionService sessionDataHolder, ISynchronizationApiClient synchronizationApiClient)
    {
        _sessionDataHolder = sessionDataHolder;
        _synchronizationApiClient = synchronizationApiClient;

        ServerInformerOperatorInfos = new Dictionary<ISynchronizationActionServerInformer.CloudActionCaller, ServerInformerOperatorInfo>();

        SyncRoot = new object();
    }

    private Dictionary<ISynchronizationActionServerInformer.CloudActionCaller, ServerInformerOperatorInfo> ServerInformerOperatorInfos { get; }
    
    private object SyncRoot { get; }

    public Task HandleCloudActionDone(SharedActionsGroup sharedActionsGroup, ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        return Task.Run(async () =>
        {
            await DoHandleCloudAction(sharedActionsGroup, cloudActionCaller);
        });
    }

    public Task HandleCloudActionError(SharedActionsGroup sharedActionsGroup)
    {
        return Task.Run(async () =>
        {
            await DoHandleCloudAction(sharedActionsGroup, _synchronizationApiClient.AssertSynchronizationActionErrors);
        });
    }

    public Task HandleCloudActionError(List<string> actionsGroupIds)
    {
        return Task.Run(async () =>
        {
            await DoHandleCloudAction(actionsGroupIds, _synchronizationApiClient.AssertSynchronizationActionErrors);
        });
    }

    public async Task HandlePendingActions()
    {
        if (_sessionDataHolder.IsCloudSession)
        {
            List<ServerInformerOperatorInfo> serverInformerOperatorInfosToHandle;
            
            lock (SyncRoot)
            {
                serverInformerOperatorInfosToHandle = ServerInformerOperatorInfos.Values.ToList();
                ServerInformerOperatorInfos.Clear();
            }
            
            await Handle(serverInformerOperatorInfosToHandle);
        }
    }

    public Task ClearPendingActions()
    {
        if (_sessionDataHolder.IsCloudSession)
        {
            lock (SyncRoot)
            {
                ServerInformerOperatorInfos.Clear();
            }
        }

        return Task.CompletedTask;
    }

    private async Task DoHandleCloudAction(List<string> actionsGroupIds, ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        if (_sessionDataHolder.IsCloudSession)
        {
            var serverInformerOperatorInfosToHandle = new List<ServerInformerOperatorInfo>();
            
            lock (SyncRoot)
            {
                Register(actionsGroupIds, cloudActionCaller);

                GetServerInformerOperatorInfosToHandle(serverInformerOperatorInfosToHandle);
            }
            
            await Handle(serverInformerOperatorInfosToHandle);
        }
    }

    private async Task DoHandleCloudAction(SharedActionsGroup sharedActionsGroup, ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        if (_sessionDataHolder.IsCloudSession)
        {
            var serverInformerOperatorInfosToHandle = new List<ServerInformerOperatorInfo>();
            
            lock (SyncRoot)
            {
                Register(sharedActionsGroup, cloudActionCaller);

                GetServerInformerOperatorInfosToHandle(serverInformerOperatorInfosToHandle);
            }
            
            await Handle(serverInformerOperatorInfosToHandle);
        }
    }

    private void GetServerInformerOperatorInfosToHandle(List<ServerInformerOperatorInfo> serverInformerOperatorInfosToHandle)
    {
        foreach (var serverInformerOperatorInfo in ServerInformerOperatorInfos.Values)
        {
            if (serverInformerOperatorInfo.ActionsCount >= 100 ||
                serverInformerOperatorInfo.CreationDate.IsOlderThan(TimeSpan.FromSeconds(15)))
            {
                serverInformerOperatorInfosToHandle.Add(serverInformerOperatorInfo);
            }
        }

        // On enlève les éléments que l'on traite
        foreach (var serverInformerOperatorInfo in serverInformerOperatorInfosToHandle)
        {
            var isRemoved = ServerInformerOperatorInfos.Remove(serverInformerOperatorInfo.CloudActionCaller);
            if (!isRemoved)
            {
                throw new Exception("Unexpected behaviour");
            }
        }
    }
    
    private void Register(List<string> actionsGroupIds, ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        if (!ServerInformerOperatorInfos.ContainsKey(cloudActionCaller))
        {
            ServerInformerOperatorInfos.Add(cloudActionCaller, new ServerInformerOperatorInfo(cloudActionCaller));
        }

        ServerInformerOperatorInfos[cloudActionCaller].Add(actionsGroupIds);
    }

    private void Register(SharedActionsGroup sharedActionsGroup, ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        if (!ServerInformerOperatorInfos.ContainsKey(cloudActionCaller))
        {
            ServerInformerOperatorInfos.Add(cloudActionCaller, new ServerInformerOperatorInfo(cloudActionCaller));
        }

        ServerInformerOperatorInfos[cloudActionCaller].Add(sharedActionsGroup.ActionsGroupId);
    }
    
    private async Task Handle(List<ServerInformerOperatorInfo> serverInformerOperatorInfos)
    {
        foreach (var serverInformerOperatorInfo in serverInformerOperatorInfos)
        {
            try
            {
                var cloudActionCaller = serverInformerOperatorInfo.CloudActionCaller;

                // https://stackoverflow.com/questions/419019/split-list-into-sublists-with-linq
                // https://stackoverflow.com/questions/15136542/parallel-foreach-with-asynchronous-lambda
                var tasks = serverInformerOperatorInfo.ActionsGroupIds.Chunk(200).Select(async chunk =>
                {
                    await cloudActionCaller.Invoke(_sessionDataHolder.SessionId!, chunk.ToList());
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to inform the server of the result of an action");
            }
        }
    }
}