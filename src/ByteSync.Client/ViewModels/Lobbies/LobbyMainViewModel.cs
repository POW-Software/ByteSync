using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using ByteSync.Business.Profiles;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Lobbies;
using ByteSync.Services.Lobbies;
using ByteSync.ViewModels.Sessions.Managing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using CloudSessionProfileDetails = ByteSync.Business.Profiles.CloudSessionProfileDetails;

namespace ByteSync.ViewModels.Lobbies;

public class LobbyMainViewModel : ActivatableViewModelBase, IRoutableViewModel
{
    private readonly ILobbyManager _lobbyManager;
    private readonly ILobbyRepository _lobbyRepository;
    private readonly ILocalizationService _localizationService;
    private readonly ILobbySynchronizationRuleViewModelFactory _lobbySynchronizationRuleViewModelFactory;

    public string? UrlPathSegment { get; }
    public IScreen HostScreen { get; }

    public LobbyMainViewModel()
    {

    }

    public LobbyMainViewModel(IScreen screen, ILobbyManager lobbyManager, ILobbyRepository lobbyRepository, ILocalizationService localizationService,
        ISessionSettingsEditViewModelFactory sessionSettingsEditViewModelFactory, 
        ILobbySynchronizationRuleViewModelFactory lobbySynchronizationRuleViewModelFactory)
    {
        HostScreen = screen;
        _lobbyManager = lobbyManager;
        _lobbyRepository = lobbyRepository;
        _localizationService = localizationService;
        _lobbySynchronizationRuleViewModelFactory = lobbySynchronizationRuleViewModelFactory;

        IsSynchronizationMode = false;
        IsInventoryMode = false;
        
        #if DEBUG
            if (Design.IsDesignMode)
            {
                SessionSettingsEditViewModel = sessionSettingsEditViewModelFactory.CreateSessionSettingsEditViewModel(SessionSettings.BuildDefault());
                LobbyId = "DesignLobbyId";
                return;
            }
        #endif

        var lobbyDetails = _lobbyRepository.GetData()!;
        
        LobbyId = lobbyDetails.LobbyId;
        IsDetailsLobbyOnly = LobbyId.StartsWith(LobbyRepository.LOCAL_LOBBY_PREFIX);
        
        Members = lobbyDetails.LobbyMembersViewModels;
        ProfileDetails = lobbyDetails.ProfileDetails;
        Profile = lobbyDetails.Profile;

        SynchronizationRules = new ObservableCollection<LobbySynchronizationRuleViewModel>();
        foreach (var cloudSessionProfileSynchronizationRule in ProfileDetails.SynchronizationRules)
        {
            var lobbySynchronizationRuleViewModel = _lobbySynchronizationRuleViewModelFactory.Create(cloudSessionProfileSynchronizationRule, ProfileDetails);
            
            SynchronizationRules.Add(lobbySynchronizationRuleViewModel);
        }
        
        CancelCommand = ReactiveCommand.CreateFromTask(Cancel);
        StartSynchronizationLobbyCommand = ReactiveCommand.CreateFromTask(StartSynchronizationLobby);
        StartInventoryLobbyCommand = ReactiveCommand.CreateFromTask(StartInventoryLobby);
        JoinLobbyCommand = ReactiveCommand.CreateFromTask(JoinLobby);
        
        ProfileName = ProfileDetails.Name;
        
        SessionSettingsEditViewModel = sessionSettingsEditViewModelFactory.CreateSessionSettingsEditViewModel(ProfileDetails.Options.Settings);

        this.WhenAnyValue(x => x.Members[0].LobbyMember.LobbyMemberInfo)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnFirstLobbyMemberSet);
        
        this.WhenActivated(OnActivated);
    }

    private async void OnActivated(CompositeDisposable disposables)
    {
        try
        {
            DisposableMixin.DisposeWith(_localizationService.CurrentCultureObservable
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => OnLocaleChanged()), disposables);
            
            IsFirstLobbyMember = await _lobbyRepository.IsFirstLobbyMember(LobbyId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during WhenActivated");
        }
    }

    private void OnFirstLobbyMemberSet(LobbyMemberInfo? lobbyMemberInfo)
    {
        if (lobbyMemberInfo == null)
        {
            IsSynchronizationMode = false;
            IsInventoryMode = false;
        }
        else
        {
            IsSynchronizationMode = lobbyMemberInfo.JoinLobbyMode == JoinLobbyModes.RunSynchronization;
            IsInventoryMode = lobbyMemberInfo.JoinLobbyMode == JoinLobbyModes.RunInventory;
        }
    }

    private async Task Cancel()
    {
        try
        {
            await _lobbyManager.ExitLobby(LobbyId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cancel");   
        }
    }
    
    private async Task StartSynchronizationLobby()
    {
        try
        {
            await _lobbyManager.StartLobbyAsync(Profile, JoinLobbyModes.RunSynchronization);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "StartSynchronizationLobby");   
        }
    }
    
    private async Task StartInventoryLobby()
    {
        try
        {
            await _lobbyManager.StartLobbyAsync(Profile, JoinLobbyModes.RunInventory);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "StartInventoryLobby");   
        }
    }
    
    private async Task JoinLobby()
    {
        try
        {
            await _lobbyManager.StartLobbyAsync(Profile, JoinLobbyModes.Join);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "JoinLobby");   
        }
    }
    
    [Reactive]
    public string ProfileName { get; set; }
    
    [Reactive]
    public string LobbyId { get; set; }
    
    [Reactive]
    public bool IsDetailsLobbyOnly { get; set; }
    
    [Reactive]
    public bool IsFirstLobbyMember { get; set; }

    [Reactive]
    public SessionSettingsEditViewModel SessionSettingsEditViewModel { get; set; }
    
    [Reactive]
    public bool IsSynchronizationMode { get; set; }
    
    [Reactive]
    public bool IsInventoryMode { get; set; }

    public CloudSessionProfileDetails ProfileDetails { get; }
    
    public CloudSessionProfile Profile { get; }

    public ObservableCollection<LobbyMemberViewModel> Members { get; }
    
    public ObservableCollection<LobbySynchronizationRuleViewModel> SynchronizationRules { get; }
    
    public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> StartSynchronizationLobbyCommand { get; }
    
    public ReactiveCommand<Unit, Unit> StartInventoryLobbyCommand { get; }
    
    public ReactiveCommand<Unit, Unit> JoinLobbyCommand { get; }

    private void OnLocaleChanged()
    {
        foreach (var lobbySynchronizationRuleViewModel in SynchronizationRules)
        {
            lobbySynchronizationRuleViewModel.OnLocaleChanged();
        }
    }
}