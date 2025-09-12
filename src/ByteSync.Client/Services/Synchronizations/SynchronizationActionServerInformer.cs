using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Common.Business.Synchronizations;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationActionServerInformer : ISynchronizationActionServerInformer
{
    private readonly ISessionService _sessionService;
    private readonly ISynchronizationApiClient _synchronizationApiClient;
    private readonly ILogger<SynchronizationActionServerInformer> _logger;

    private const int READY_COUNT_THRESHOLD = 100;
    private const int SEND_CHUNK_SIZE = 200;
    
    private static readonly TimeSpan _maxBatchAge = TimeSpan.FromSeconds(15);

    public SynchronizationActionServerInformer(ISessionService sessionService,
        ISynchronizationApiClient synchronizationApiClient, ILogger<SynchronizationActionServerInformer> logger)
    {
        _sessionService = sessionService;
        _synchronizationApiClient = synchronizationApiClient;
        _logger = logger;

        ServerInformerOperatorInfos = new Dictionary<ISynchronizationActionServerInformer.CloudActionCaller, ServerInformerData>();

        SyncRoot = new object();
    }

    private Dictionary<ISynchronizationActionServerInformer.CloudActionCaller, ServerInformerData> ServerInformerOperatorInfos { get; }
    
    private object SyncRoot { get; }

    public async Task HandleCloudActionDone(SharedActionsGroup sharedActionsGroup, SharedDataPart localTarget, ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        await DoHandleCloudAction(sharedActionsGroup, localTarget, cloudActionCaller, null);
    }

    public async Task HandleCloudActionDone(SharedActionsGroup sharedActionsGroup, SharedDataPart localTarget, ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller,
        Dictionary<string, SynchronizationActionMetrics>? actionMetricsByActionId)
    {
        await DoHandleCloudAction(sharedActionsGroup, localTarget, cloudActionCaller, actionMetricsByActionId);
    }
    
    public async Task HandleCloudActionError(SharedActionsGroup sharedActionsGroup, SharedDataPart localTarget)
    {
        await DoHandleCloudAction(sharedActionsGroup, localTarget, _synchronizationApiClient.InformSynchronizationActionErrors, null);
    }

    public async Task HandleCloudActionError(SharedActionsGroup sharedActionsGroup)
    {
        await DoHandleCloudAction(sharedActionsGroup, null, _synchronizationApiClient.InformSynchronizationActionErrors, null);
    }

    public async Task HandleCloudActionError(List<string> actionsGroupIds)
    {
        await DoHandleCloudAction(actionsGroupIds, null, _synchronizationApiClient.InformSynchronizationActionErrors, null);
    }

    public async Task HandlePendingActions()
    {
        if (_sessionService.IsCloudSession)
        {
            List<ServerInformerData> serverInformerOperatorInfosToHandle;

            lock (SyncRoot)
            {
                serverInformerOperatorInfosToHandle = new List<ServerInformerData>();

                foreach (var info in ServerInformerOperatorInfos.Values)
                {
                    serverInformerOperatorInfosToHandle.AddRange(info.ExtractAllSlices(SEND_CHUNK_SIZE));
                }
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
        ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller,
        Dictionary<string, SynchronizationActionMetrics>? actionMetricsByActionId)
    {
        await DoHandleCloudAction([sharedActionsGroup.ActionsGroupId], localTarget, cloudActionCaller, actionMetricsByActionId);
    }
    
    private void RegisterTargetCloudAction(List<string> actionsGroupIds, SharedDataPart? localTarget,
        ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller,
        Dictionary<string, SynchronizationActionMetrics>? actionMetricsByActionId)
    {
        if (!ServerInformerOperatorInfos.ContainsKey(cloudActionCaller))
        {
            ServerInformerOperatorInfos.Add(cloudActionCaller, new ServerInformerData(cloudActionCaller));
        }

        ServerInformerOperatorInfos[cloudActionCaller].Add(actionsGroupIds, localTarget?.NodeId, actionMetricsByActionId);
    }

    private async Task DoHandleCloudAction(List<string> actionsGroupIds, SharedDataPart? localTarget,
        ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller,
        Dictionary<string, SynchronizationActionMetrics>? actionMetricsByActionId)
    {
        if (_sessionService.IsCloudSession)
        {
            var serverInformerOperatorInfosToHandle = new List<ServerInformerData>();
            
            lock (SyncRoot)
            {
                RegisterTargetCloudAction(actionsGroupIds, localTarget, cloudActionCaller, actionMetricsByActionId);

                GetServerInformerOperatorInfosToHandle(serverInformerOperatorInfosToHandle);
            }
            
            await Handle(serverInformerOperatorInfosToHandle);
        }
    }

    private void GetServerInformerOperatorInfosToHandle(List<ServerInformerData> serverInformerOperatorInfosToHandle)
    {
        foreach (var info in ServerInformerOperatorInfos.Values)
        {
            var slices = info.ExtractReadySlices(READY_COUNT_THRESHOLD, _maxBatchAge, SEND_CHUNK_SIZE);
            serverInformerOperatorInfosToHandle.AddRange(slices);
        }
    }
    
    private async Task Handle(List<ServerInformerData> serverInformerOperatorInfos)
    {
        foreach (var serverInformerOperatorInfo in serverInformerOperatorInfos)
        {
            try
            {
                var cloudActionCaller = serverInformerOperatorInfo.CloudActionCaller;

                var tasks = serverInformerOperatorInfo.SynchronizationActionRequests.Chunk(SEND_CHUNK_SIZE).Select(async chunk =>
                {
                    foreach (var synchronizationActionRequest in chunk.ToList())
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
