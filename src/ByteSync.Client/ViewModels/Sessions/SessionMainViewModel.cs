using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using ByteSync.Business;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.ViewModels.Sessions.Cloud.Managing;
using ByteSync.ViewModels.Sessions.Cloud.Members;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Inventories;
using ByteSync.ViewModels.Sessions.Local;
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
        ComparisonResultViewModel comparisonResultViewModel, SynchronizationMainViewModel synchronizationMainViewModel, 
        ISessionMachineViewModelFactory sessionMachineViewModelFactory, ISessionMemberRepository sessionMemberRepository)
    {
        HostScreen = screen;
        Activator = new ViewModelActivator();

        _sessionService = sessionService;
        _sessionMachineViewModelFactory = sessionMachineViewModelFactory;
        _sessionMemberRepository = sessionMemberRepository;
        
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
                .Subscribe(SessionModeChanged)
                .DisposeWith(disposables);
            
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
            
            _sessionMemberRepository.SortedSessionMembersObservable
                .Select(m => m.Count > 0)
                .DistinctUntilChanged()
                .Subscribe(_ => OnCloudSessionJoined())
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

            if (_sessionService.IsCloudSession)
            {
                OnCloudSessionJoined();
            }
        });
    }

    private void SessionModeChanged(SessionModes? sessionMode)
    {
        if (sessionMode == SessionModes.Cloud)
        {
            StartOrJoinViewModel = Services.ContainerProvider.Container.Resolve<StartOrJoinViewModel>();
            
            JoinCloudSessionViewModel = Services.ContainerProvider.Container.Resolve<JoinCloudSessionViewModel>();
            
            StartCloudSessionViewModel = Services.ContainerProvider.Container.Resolve<StartCloudSessionViewModel>();
            CurrentCloudSessionViewModel = Services.ContainerProvider.Container.Resolve<CurrentCloudSessionViewModel>();
            
            StartOrJoinViewModel.JoinComparisonCommand = ReactiveCommand
                .Create(() => { CloudSessionManagement = JoinCloudSessionViewModel; });
            StartOrJoinViewModel.StartComparisonCommand = ReactiveCommand.CreateFromTask(CreateSession);
            
            CloudSessionManagement = StartOrJoinViewModel;
        }
        else if (sessionMode == SessionModes.Local)
        {
            LocalSessionManagement = Services.ContainerProvider.Container.Resolve<LocalSessionManagementViewModel>();
        }
    }

    public StartOrJoinViewModel? StartOrJoinViewModel { get; private set; }
        
    public JoinCloudSessionViewModel? JoinCloudSessionViewModel { get; private set; }
        
    public StartCloudSessionViewModel? StartCloudSessionViewModel { get; private set; }
        
    public CurrentCloudSessionViewModel? CurrentCloudSessionViewModel { get; private set; }
    
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
    public ViewModelBase? LocalSessionParts { get; set; }
    
    [Reactive]
    public ViewModelBase InventoryProcess { get; set; }
        
    [Reactive]
    public ComparisonResultViewModel ComparisonResult { get; set; }
        
    [Reactive]
    public ViewModelBase SynchronizationProcess { get; set; }
    
    private async Task CreateSession()
    {
        CloudSessionManagement = StartCloudSessionViewModel;
        await StartCloudSessionViewModel!.CreateSession();
    }

    private void OnCloudSessionJoined()
    {
        CloudSessionManagement = CurrentCloudSessionViewModel;
    }
}