using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Repositories;
using DynamicData;

namespace ByteSync.Repositories;

public class SharedActionsGroupRepository : BaseSourceCacheRepository<SharedActionsGroup, string>, ISharedActionsGroupRepository
{
    private readonly ISessionInvalidationSourceCachePolicy<SharedActionsGroup, string> _sessionInvalidationSourceCachePolicy;

    public SharedActionsGroupRepository(ISessionInvalidationSourceCachePolicy<SharedActionsGroup, string> sessionInvalidationSourceCachePolicy)
    {
        OrganizedSharedActionsGroups = new List<SharedActionsGroup>();
        
        _sessionInvalidationSourceCachePolicy = sessionInvalidationSourceCachePolicy;
        _sessionInvalidationSourceCachePolicy.Initialize(SourceCache, true, true);
    }
    
    protected override string KeySelector(SharedActionsGroup sharedAtomicAction) => sharedAtomicAction.ActionsGroupId;

    public IObservableCache<SharedActionsGroup, string> SharedActionsGroups => ObservableCache;

    public IEnumerable<SharedActionsGroup> SharedActionsGroupsList => SourceCache.Items;

    public List<SharedActionsGroup> OrganizedSharedActionsGroups { get; private set; }
    
    public List<ActionsGroupDefinition> GetActionsGroupsDefinitions()
    {
        List<ActionsGroupDefinition> result = new List<ActionsGroupDefinition>();
        
        foreach (var sharedActionsGroup in SharedActionsGroupsList)
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
        var result = SourceCache.Items.Single(i => i.ActionsGroupId == actionsGroupId);
        return result;
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