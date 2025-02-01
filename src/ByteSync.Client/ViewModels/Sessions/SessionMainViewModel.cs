using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Business;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Repositories;
using ByteSync.ViewModels.Sessions.Cloud.Managing;
using ByteSync.ViewModels.Sessions.Cloud.Members;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Inventories;
using ByteSync.ViewModels.Sessions.Synchronizations;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions;

class SessionMainViewModel : ViewModelBase, IRoutableViewModel, IActivatableViewModel 
{
    public string? UrlPathSegment { get; }
        
    public IScreen HostScreen { get; }

    public ViewModelActivator Activator { get; }
    
    private readonly ISessionService _sessionService;
    
    private ReadOnlyObservableCollection<SessionMachineViewModel> _data;
    private readonly ISessionMachineViewModelFactory _sessionMachineViewModelFactory;
    private readonly ISessionMemberRepository _sessionMemberRepository;

    public SessionMainViewModel(IScreen screen, ISessionService sessionService, InventoryProcessViewModel inventoryProcessViewModel, 
        ComparisonResultViewModel comparisonResultViewModel, CurrentCloudSessionViewModel currentCloudSessionViewModel, 
        SynchronizationMainViewModel synchronizationMainViewModel, ISessionMachineViewModelFactory sessionMachineViewModelFactory, 
        ISessionMemberRepository sessionMemberRepository)
    {
        HostScreen = screen;
        Activator = new ViewModelActivator();

        _sessionService = sessionService;
        _sessionMachineViewModelFactory = sessionMachineViewModelFactory;
        _sessionMemberRepository = sessionMemberRepository;
        
        CloudSessionManagement = currentCloudSessionViewModel;
        InventoryProcess = inventoryProcessViewModel;
        ComparisonResult = comparisonResultViewModel;
        SynchronizationProcess = synchronizationMainViewModel;
        
        var sessionMemberCache = _sessionMemberRepository.ObservableCache.Connect()
            .Transform(smi => _sessionMachineViewModelFactory.CreateSessionMachineViewModel(smi))
            .AutoRefresh(vm => vm.JoinedSessionOn)
            .Sort(SortExpressionComparer<SessionMachineViewModel>.Ascending(vm => vm.JoinedSessionOn))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _data)
            .DisposeMany()
            .Subscribe();

        this.WhenActivated(disposables =>
        {
            sessionMemberCache.DisposeWith(disposables);
            
            _sessionService.SessionMode
                .Where(x => x != null)
                .Select(x => x == SessionModes.Cloud)
                .ToPropertyEx(this, x => x.IsCloudSessionMode)
                .DisposeWith(disposables);
                
            _sessionService.SessionObservable
                .Where(x => x != null)
                .Select(x => x is CloudSession)
                .ToPropertyEx(this, x => x.IsCloudSession)
                .DisposeWith(disposables);
            
            _sessionService.SessionStatusObservable.CombineLatest(_sessionMemberRepository.SortedSessionMembersObservable)
                .Select(s => (s.First == SessionStatus.Preparation && s.Second.Count > 0)
                                || (s.First == SessionStatus.Inventory 
                                || s.First == SessionStatus.Comparison
                                || s.First == SessionStatus.Synchronization)
                             )
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.IsInventoryVisible)
                .DisposeWith(disposables);
            
            _sessionService.SessionStatusObservable
                .Select(s => s is SessionStatus.Inventory or SessionStatus.Comparison or SessionStatus.Synchronization)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.IsComparisonVisible)
                .DisposeWith(disposables);
            
            _sessionService.SessionStatusObservable
                .Select(s => s is SessionStatus.Comparison or SessionStatus.Synchronization)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.IsSynchronizationVisible)
                .DisposeWith(disposables);
        });
    }
    
    public ReadOnlyObservableCollection<SessionMachineViewModel> Machines => _data;

    [Reactive]
    public ViewModelBase? CloudSessionManagement { get; set; }
    
    [Reactive]
    public ViewModelBase? LocalSessionManagement { get; set; }
    
    public extern bool IsCloudSessionMode { [ObservableAsProperty] get; }
    
    public extern bool IsCloudSession { [ObservableAsProperty] get; }

    public extern bool IsInventoryVisible { [ObservableAsProperty] get; }
        
    public extern bool IsComparisonVisible { [ObservableAsProperty] get; }
    
    public extern bool IsSynchronizationVisible { [ObservableAsProperty] get; }
    
    [Reactive]
    public ViewModelBase InventoryProcess { get; set; }
        
    [Reactive]
    public ComparisonResultViewModel ComparisonResult { get; set; }
        
    [Reactive]
    public ViewModelBase SynchronizationProcess { get; set; }
}