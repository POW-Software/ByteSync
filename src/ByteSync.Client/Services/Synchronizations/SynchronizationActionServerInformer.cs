using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationActionServerInformer : ISynchronizationActionServerInformer
{
    private readonly ISessionService _sessionService;
    private readonly ISynchronizationApiClient _synchronizationApiClient;
    private readonly ILogger<SynchronizationActionServerInformer> _logger;

    public SynchronizationActionServerInformer(ISessionService sessionService,
        ISynchronizationApiClient synchronizationApiClient, ILogger<SynchronizationActionServerInformer> logger)
    {
        _sessionService = sessionService;
        _synchronizationApiClient = synchronizationApiClient;
        _logger = logger;

        ServerInformerOperatorInfos = new Dictionary<ISynchronizationActionServerInformer.CloudActionCaller, ServerInformerOperatorInfo>();

        SyncRoot = new object();
    }

    private Dictionary<ISynchronizationActionServerInformer.CloudActionCaller, ServerInformerOperatorInfo> ServerInformerOperatorInfos { get; }
    
    private object SyncRoot { get; }

    public async Task HandleCloudActionDone(SharedActionsGroup sharedActionsGroup, SharedDataPart localTarget, ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        await DoHandleCloudAction(sharedActionsGroup, localTarget, cloudActionCaller);
    }
    
    public async Task HandleCloudActionError(SharedActionsGroup sharedActionsGroup, SharedDataPart localTarget)
    {
        await DoHandleCloudAction(sharedActionsGroup, localTarget, _synchronizationApiClient.InformSynchronizationActionErrors);
    }

    public async Task HandleCloudActionError(SharedActionsGroup sharedActionsGroup)
    {
        await DoHandleCloudAction(sharedActionsGroup, null, _synchronizationApiClient.InformSynchronizationActionErrors);
    }

    public async Task HandleCloudActionError(List<string> actionsGroupIds)
    {
        await DoHandleCloudAction(actionsGroupIds, null, _synchronizationApiClient.InformSynchronizationActionErrors);
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

    private async Task DoHandleCloudAction(SharedActionsGroup sharedActionsGroup, SharedDataPart? localTarget,
        ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        await DoHandleCloudAction([sharedActionsGroup.ActionsGroupId], localTarget, cloudActionCaller);
    }
    
    private void RegisterTargetCloudAction(List<string> actionsGroupIds, SharedDataPart? localTarget,
        ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        if (!ServerInformerOperatorInfos.ContainsKey(cloudActionCaller))
        {
            ServerInformerOperatorInfos.Add(cloudActionCaller, new ServerInformerOperatorInfo(cloudActionCaller));
        }

        var synchronizationActionRequest = new SynchronizationActionRequest(actionsGroupIds, localTarget?.NodeId);
            
        ServerInformerOperatorInfos[cloudActionCaller].Add(synchronizationActionRequest);
    }

    private async Task DoHandleCloudAction(List<string> actionsGroupIds, SharedDataPart? localTarget,
        ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        if (_sessionService.IsCloudSession)
        {
            var serverInformerOperatorInfosToHandle = new List<ServerInformerOperatorInfo>();
            
            lock (SyncRoot)
            {
                RegisterTargetCloudAction(actionsGroupIds, localTarget, cloudActionCaller);

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
    
    private async Task Handle(List<ServerInformerOperatorInfo> serverInformerOperatorInfos)
    {
        foreach (var serverInformerOperatorInfo in serverInformerOperatorInfos)
        {
            try
            {
                var cloudActionCaller = serverInformerOperatorInfo.CloudActionCaller;

                var tasks = serverInformerOperatorInfo.SynchronizationActionRequests.Chunk(200).Select(async chunk =>
                {
                    foreach (var synchronizationActionRequest in  chunk.ToList())
                    {
                        await cloudActionCaller.Invoke(_sessionService.SessionId!, synchronizationActionRequest);
                    }
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