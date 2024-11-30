using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;
using ByteSync.Interfaces.Controls.Actions;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationStartFactory : ISynchronizationStartFactory
{
    private readonly ISharedAtomicActionComputer _sharedAtomicActionComputer;
    private readonly ISynchronizationRulesService _synchronizationRulesService;
    private readonly ISharedActionsGroupComputer _sharedActionsGroupComputer;
    private readonly ISessionService _sessionService;
    private readonly IConnectionService _connectionService;
    private readonly ISharedActionsGroupOrganizer _sharedActionsGroupOrganizer;
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;

    public SynchronizationStartFactory(ISharedAtomicActionComputer sharedAtomicActionComputer,
        ISynchronizationRulesService synchronizationRulesService, ISharedActionsGroupComputer sharedActionsGroupComputer,
        ISessionService sessionService, IConnectionService connectionService, ISharedActionsGroupOrganizer sharedActionsGroupOrganizer,
        ISharedActionsGroupRepository sharedActionsGroupRepository)
    {
        _sharedAtomicActionComputer = sharedAtomicActionComputer;
        _synchronizationRulesService = synchronizationRulesService;
        _sharedActionsGroupComputer = sharedActionsGroupComputer;
        _sessionService = sessionService;
        _connectionService = connectionService;
        _sharedActionsGroupOrganizer = sharedActionsGroupOrganizer;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;
    }
    
    public async Task<SharedSynchronizationStartData> PrepareSharedData()
    {
        var sharedAtomicActions = await _sharedAtomicActionComputer.ComputeSharedAtomicActions();

        await _sharedActionsGroupComputer.ComputeSharedActionsGroups();

        var sharedActionsGroups = _sharedActionsGroupRepository.Elements.ToList();

        await _sharedActionsGroupOrganizer.OrganizeSharedActionGroups();
        
        var looseSynchronizationRules = _synchronizationRulesService.GetLooseSynchronizationRules();
        
        var synchronizationStartData = new SharedSynchronizationStartData(
            _sessionService.SessionId!, _connectionService.CurrentEndPoint!,
            sharedAtomicActions, sharedActionsGroups, looseSynchronizationRules);
        
        synchronizationStartData.TotalVolumeToProcess = sharedActionsGroups.Sum(ssa => ssa.Size.GetValueOrDefault());
        synchronizationStartData.TotalActionsToProcess = sharedActionsGroups.Count;

        return synchronizationStartData;
    }
}