using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Synchronizations;

namespace ByteSync.Interfaces.Repositories;

public interface ISharedActionsGroupRepository : IBaseSourceCacheRepository<SharedActionsGroup, string>
{
    List<SharedActionsGroup> OrganizedSharedActionsGroups { get; }
    
    List<ActionsGroupDefinition> GetActionsGroupsDefinitions();
    
    void SetSharedActionsGroups(List<SharedActionsGroup> sharedActionsGroups);

    void SetOrganizedSharedActionsGroups(List<SharedActionsGroup> organizedSharedActionGroups);
    
    SharedActionsGroup GetSharedActionsGroup(string actionsGroupId);
    
    Task OnSynchronizationProgressChanged(SynchronizationProgressPush synchronizationProgressPush);
}