using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Repositories;
using DynamicData;

namespace ByteSync.Repositories;

public class SharedActionsGroupRepository : BaseSourceCacheRepository<SharedActionsGroup, string>, ISharedActionsGroupRepository
{
    private readonly ISessionInvalidationCachePolicy<SharedActionsGroup, string> _sessionInvalidationCachePolicy;
    private readonly ILogger<SharedActionsGroupRepository> _logger;

    public SharedActionsGroupRepository(ISessionInvalidationCachePolicy<SharedActionsGroup, string> sessionInvalidationCachePolicy,
        ILogger<SharedActionsGroupRepository> logger)
    {
        _logger = logger;
        
        OrganizedSharedActionsGroups = [];
        
        _sessionInvalidationCachePolicy = sessionInvalidationCachePolicy;
        _sessionInvalidationCachePolicy.Initialize(SourceCache, true, true);
    }
    
    protected override string KeySelector(SharedActionsGroup sharedAtomicAction) => sharedAtomicAction.ActionsGroupId;

    public List<SharedActionsGroup> OrganizedSharedActionsGroups { get; private set; }
    
    public List<ActionsGroupDefinition> GetActionsGroupsDefinitions()
    {
        List<ActionsGroupDefinition> result = [];
        
        foreach (var sharedActionsGroup in SourceCache.Items)
        {
            result.Add(sharedActionsGroup.GetDefinition());
        }

        return result;
    }

    public void SetSharedActionsGroups(List<SharedActionsGroup> sharedActionsGroups)
    {
        Clear();
        AddOrUpdate(sharedActionsGroups);
    }

    public void SetOrganizedSharedActionsGroups(List<SharedActionsGroup> organizedSharedActionGroups)
    {
        OrganizedSharedActionsGroups = organizedSharedActionGroups.ToList();
    }
    
    public SharedActionsGroup GetSharedActionsGroup(string actionsGroupId)
    {
        try
        {
            var result = SourceCache.Items.Single(i => i.ActionsGroupId == actionsGroupId);
            return result;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error getting shared actions group with id {ActionsGroupId}", actionsGroupId);
            throw;
        }
    }

    public Task OnSynchronizationProgressChanged(SynchronizationProgressPush synchronizationProgressPush)
    {
        if (synchronizationProgressPush.TrackingActionSummaries == null)
        {
            return Task.CompletedTask;
        }
        
        foreach (var trackingActionSummary in synchronizationProgressPush.TrackingActionSummaries)
        {
            var sharedActionsGroup = GetSharedActionsGroup(trackingActionSummary.ActionsGroupId);

            if (trackingActionSummary.IsSuccess)
            {
                sharedActionsGroup.SynchronizationStatus = SynchronizationStatus.Success;
            }
            else if (trackingActionSummary.IsError)
            {
                sharedActionsGroup.SynchronizationStatus = SynchronizationStatus.Error;
            }
            
            SourceCache.AddOrUpdate(sharedActionsGroup);
        }
        
        return Task.CompletedTask;
    }
}