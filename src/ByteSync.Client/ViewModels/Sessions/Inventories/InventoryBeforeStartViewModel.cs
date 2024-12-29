using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSync.Assets.Resources;
using ByteSync.Business.Arguments;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Repositories;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.Sessions.Inventories;

public class InventoryBeforeStartViewModel : ActivatableViewModelBase
{
    private readonly ILocalizationService _localizationService;
    private readonly ISessionService _sessionService;
    private readonly IDataInventoryStarter _dataInventoryStarter;
    private readonly ISessionMemberRepository _sessionMemberRepository;

    public InventoryBeforeStartViewModel()
    {
        
    } 
    
    public InventoryBeforeStartViewModel(ILocalizationService localizationService, ISessionService sessionService, 
        IDataInventoryStarter dataInventoryStarter, ISessionMemberRepository sessionMemberRepository)
    {
        _localizationService = localizationService;
        _sessionService = sessionService;
        _dataInventoryStarter = dataInventoryStarter;
        _sessionMemberRepository = sessionMemberRepository;
        
        StartInventoryCommand = ReactiveCommand.CreateFromTask<bool>(StartDataInventory);

        this.WhenActivated(HandleActivation);
    }

    private void HandleActivation(CompositeDisposable disposables)
    {
        UpdateWaitingForInventoryStartMessage();

        _dataInventoryStarter.CanCurrentUserStartInventory()
            .ToPropertyEx(this, x => x.CanCurrentUserStartInventory)
            .DisposeWith(disposables);

        _localizationService.CurrentCultureObservable
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => OnLocaleChanged())
            .DisposeWith(disposables);
    }
    
    public InventoryProcessViewModel InventoryProcess { get; set; }
    
    public extern bool CanCurrentUserStartInventory { [ObservableAsProperty] get; }

    private StartInventoryStatuses? LastStartInventoryStatus { get; set; }
    
    public ReactiveCommand<bool, Unit> StartInventoryCommand { get; set; }

    [Reactive]
    public string WaitingForInventoryStartMessage { get; set; }
    
    [Reactive]
    public string StartInventoryErrorMessage { get; set; }

    private void UpdateWaitingForInventoryStartMessage()
    {
        if (_sessionService.CurrentRunSessionProfileInfo is { AutoStartsInventory: true })
        {
            if (_sessionService.IsCloudSession)
            {
                WaitingForInventoryStartMessage = _localizationService[nameof(Resources.InventoryProcess_WaitingForAutomaticStart_CloudSession)];
            }
            else
            {
                WaitingForInventoryStartMessage = _localizationService[nameof(Resources.InventoryProcess_WaitingForAutomaticStart_LocalSession)];
            }
        }
        else
        {
            if (_sessionService.IsCloudSession)
            {
                if (!_sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue)
                {
                    var creatorMachineName = _sessionMemberRepository.Elements.First().MachineName;

                #if DEBUG
                    if (Environment.GetCommandLineArgs().Contains(DebugArguments.SHOW_DEMO_DATA))
                    {
                        creatorMachineName = "MACHINE_NAME_1";
                    }
                #endif

                    WaitingForInventoryStartMessage = String.Format(_localizationService[nameof(Resources.InventoryProcess_WaitingForClientATemplate)], creatorMachineName);
                }
            }
        }
    }
    
    private void UpdateStartInventoryErrorMessage()
    {
        StartInventoryErrorMessage = LastStartInventoryStatus switch
        {
            null => "",
            StartInventoryStatuses.LessThan2Members => _localizationService[
                nameof(Resources.InventoryProcess_LessThan2Members)],
            
            StartInventoryStatuses.LessThan2MembersWithDataToSynchronize => _localizationService[
                nameof(Resources.InventoryProcess_LessThan2MembersWithDataToSynchronize)],
            
            StartInventoryStatuses.UndefinedSession => _localizationService[
                nameof(Resources.InventoryProcess_UndefinedSession)],
            
            StartInventoryStatuses.UndefinedSettings => _localizationService[
                nameof(Resources.InventoryProcess_UndefinedSettings)],
            
            StartInventoryStatuses.SessionNotFound => _localizationService[
                nameof(Resources.InventoryProcess_SessionNotFound)],
            
            StartInventoryStatuses.MoreThan5Members => _localizationService[
                nameof(Resources.InventoryProcess_MoreThan5Members)],
            
            StartInventoryStatuses.LessThan2DataSources => _localizationService[
                nameof(Resources.InventoryProcess_LessThan2DataSources)],
            
            StartInventoryStatuses.MoreThan5DataSources => _localizationService[
                nameof(Resources.InventoryProcess_MoreThan5DataSources)],
            
            StartInventoryStatuses.AtLeastOneMemberWithNoDataToSynchronize => _localizationService[
                nameof(Resources.InventoryProcess_AtLeastOneMemberWithNoDataToSynchronize)],
            
            StartInventoryStatuses.UnknownError => _localizationService[
                nameof(Resources.InventoryProcess_UnknownError)],
            
            _ => _localizationService[nameof(Resources.InventoryProcess_UnknownError)]
        };
    }
    
    private void OnLocaleChanged()
    {
        UpdateStartInventoryErrorMessage();
    }
    
    private async Task StartDataInventory(bool isLaunchedByUser)
    {
        try
        {
            LastStartInventoryStatus = null;
            UpdateStartInventoryErrorMessage();
            
            var result = await _dataInventoryStarter.StartDataInventory(true);

            if (!result.IsOK)
            {
                Log.Warning("Cannot start the Data Inventory: {reason}", result.Status);

                LastStartInventoryStatus = result.Status;
                UpdateStartInventoryErrorMessage();
            }
        }
        catch (Exception ex)
        {
            LastStartInventoryStatus = StartInventoryStatuses.UnknownError;
            Log.Error(ex, "InventoryProcessViewModel.StartDataInventory");
        }
        // finally
        // {
        //     RemainingTimeComputer.Stop();
        // }
    }
}