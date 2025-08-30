using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Interfaces.Services.Communications;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationActionServerInformer : ISynchronizationActionServerInformer
{
    private readonly ISessionService _sessionService;
    private readonly IConnectionService _connectionService;
    private readonly ISynchronizationApiClient _synchronizationApiClient;
    private readonly ILogger<SynchronizationActionServerInformer> _logger;

    public SynchronizationActionServerInformer(ISessionService sessionService, IConnectionService connectionService, 
        ISynchronizationApiClient synchronizationApiClient, ILogger<SynchronizationActionServerInformer> logger)
    {
        _sessionService = sessionService;
        _connectionService = connectionService;
        _synchronizationApiClient = synchronizationApiClient;
        _logger = logger;

        ServerInformerOperatorInfos = new Dictionary<ISynchronizationActionServerInformer.CloudActionCaller, ServerInformerOperatorInfo>();

        SyncRoot = new object();
    }

    private Dictionary<ISynchronizationActionServerInformer.CloudActionCaller, ServerInformerOperatorInfo> ServerInformerOperatorInfos { get; }
    
    private object SyncRoot { get; }

    public Task HandleCloudActionDone(SharedActionsGroup sharedActionsGroup, ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        return Task.Run(async () =>
        {
            await DoHandleTargetCloudAction(sharedActionsGroup, cloudActionCaller);
        });
    }

    public Task HandleCloudActionError(SharedActionsGroup sharedActionsGroup)
    {
        return Task.Run(async () =>
        {
            await DoHandleErrorCloudAction(sharedActionsGroup, _synchronizationApiClient.AssertSynchronizationActionErrors);
        });
    }

    public Task HandleCloudActionError(List<string> actionsGroupIds)
    {
        // TODO
        return Task.CompletedTask;
        
        // return Task.Run(async () =>
        // {
        //     await DoHandleCloudAction(actionsGroupIds, _synchronizationApiClient.AssertSynchronizationActionErrors);
        // });
    }

    public async Task HandlePendingActions()
    {
        if (_sessionService.IsCloudSession)
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
        if (_sessionService.IsCloudSession)
        {
            lock (SyncRoot)
            {
                ServerInformerOperatorInfos.Clear();
            }
        }

        return Task.CompletedTask;
    }

    // private async Task DoHandleTargetCloudAction(List<string> actionsGroupIds, ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    // {
    //     if (_sessionService.IsCloudSession)
    //     {
    //         var serverInformerOperatorInfosToHandle = new List<ServerInformerOperatorInfo>();
    //         
    //         lock (SyncRoot)
    //         {
    //             Register(actionsGroupIds, cloudActionCaller);
    //
    //             GetServerInformerOperatorInfosToHandle(serverInformerOperatorInfosToHandle);
    //         }
    //         
    //         await Handle(serverInformerOperatorInfosToHandle);
    //     }
    // }
    
    private async Task DoHandleErrorCloudAction(SharedActionsGroup sharedActionsGroup, ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        if (_sessionService.IsCloudSession)
        {
            var serverInformerOperatorInfosToHandle = new List<ServerInformerOperatorInfo>();
            
            lock (SyncRoot)
            {
                RegisterTargetCloudAction(sharedActionsGroup, cloudActionCaller);

                GetServerInformerOperatorInfosToHandle(serverInformerOperatorInfosToHandle);
            }
            
            await Handle(serverInformerOperatorInfosToHandle);
        }
    }

    private async Task DoHandleTargetCloudAction(SharedActionsGroup sharedActionsGroup, ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        if (_sessionService.IsCloudSession)
        {
            var serverInformerOperatorInfosToHandle = new List<ServerInformerOperatorInfo>();
            
            lock (SyncRoot)
            {
                RegisterTargetCloudAction(sharedActionsGroup, cloudActionCaller);

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

        // We remove the elements we are processing
        foreach (var serverInformerOperatorInfo in serverInformerOperatorInfosToHandle)
        {
            var isRemoved = ServerInformerOperatorInfos.Remove(serverInformerOperatorInfo.CloudActionCaller);
            if (!isRemoved)
            {
                throw new Exception("Unexpected behaviour");
            }
        }
    }
    
    // private void Register(List<string> actionsGroupIds, ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    // {
    //     if (!ServerInformerOperatorInfos.ContainsKey(cloudActionCaller))
    //     {
    //         ServerInformerOperatorInfos.Add(cloudActionCaller, new ServerInformerOperatorInfo(cloudActionCaller));
    //     }
    //
    //     ServerInformerOperatorInfos[cloudActionCaller].Add(actionsGroupIds);
    // }

    private void RegisterTargetCloudAction(SharedActionsGroup sharedActionsGroup, ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        // var nodeId = sharedActionsGroup.GetCurrentNodeId(_connectionService.CurrentEndPoint!);
        
        if (!ServerInformerOperatorInfos.ContainsKey(cloudActionCaller))
        {
            ServerInformerOperatorInfos.Add(cloudActionCaller, new ServerInformerOperatorInfo(cloudActionCaller));
        }

        // // Mettre à jour le NodeId s'il n'était pas défini
        // if (ServerInformerOperatorInfos[cloudActionCaller].NodeId == null && nodeId != null)
        // {
        //     ServerInformerOperatorInfos[cloudActionCaller].NodeId = nodeId;
        // }

        foreach (var targetDataPart in sharedActionsGroup.Targets)
        {
            var synchronizationActionRequest = new SynchronizationActionRequest(new List<string> { sharedActionsGroup.ActionsGroupId }, targetDataPart.NodeId);
            
            ServerInformerOperatorInfos[cloudActionCaller].Add(synchronizationActionRequest);
        }
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
                var tasks = serverInformerOperatorInfo.SynchronizationActionRequests.Chunk(200).Select(async chunk =>
                {
                    foreach (var synchronizationActionRequest in  chunk.ToList())
                    {
                        await cloudActionCaller.Invoke(_sessionService.SessionId!, synchronizationActionRequest);
                    }
                    
                    // await cloudActionCaller.Invoke(_sessionService.SessionId!, chunk.ToList(), serverInformerOperatorInfo.NodeId);
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to inform the server of the result of an action");
            }
        }
    }
}