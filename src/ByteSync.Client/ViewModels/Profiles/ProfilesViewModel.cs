using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Interfaces.Controls.Automating;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Lobbies;
using ByteSync.Interfaces.Profiles;
using ReactiveUI;
using Serilog;
using Splat;

namespace ByteSync.ViewModels.Profiles;

public class ProfilesViewModel : ActivableViewModelBase
{
    private readonly ISessionProfileLocalDataManager _sessionProfileLocalDataManager;
    private readonly ILobbyManager _lobbyManager;
    private readonly ISessionProfileManager _profileManager;
    private readonly IDialogService _dialogService;
    private readonly ICommandLineModeHandler _commandLineModeHandler;

    public ProfilesViewModel() : this(null)
    {

    }

    public ProfilesViewModel(ISessionProfileLocalDataManager? sessionProfileLocalDataManager,
        ILobbyManager? lobbyManager = null, ISessionProfileManager? profileManager = null,
        IDialogService? dialogService = null, ICommandLineModeHandler? commandLineModeHandler = null)
    {
        _sessionProfileLocalDataManager = sessionProfileLocalDataManager ?? Locator.Current.GetService<ISessionProfileLocalDataManager>()!;
        _lobbyManager = lobbyManager ?? Locator.Current.GetService<ILobbyManager>()!;
        _profileManager = profileManager ?? Locator.Current.GetService<ISessionProfileManager>()!;
        _dialogService = dialogService ?? Locator.Current.GetService<IDialogService>()!;
        _commandLineModeHandler = commandLineModeHandler ?? Locator.Current.GetService<ICommandLineModeHandler>()!;

        Profiles = new ObservableCollection<ProfileViewModel>();
        
        StartProfileSynchronizationCommand = ReactiveCommand.CreateFromTask<ProfileViewModel>(StartProfileSynchronization);
        StartProfileInventoryCommand = ReactiveCommand.CreateFromTask<ProfileViewModel>(StartProfileInventory);
        ShowProfileDetailsCommand = ReactiveCommand.CreateFromTask<ProfileViewModel>(ShowProfileDetails);
        JoinProfileCommand = ReactiveCommand.CreateFromTask<ProfileViewModel>(JoinProfile);
        DeleteProfileCommand = ReactiveCommand.CreateFromTask<ProfileViewModel>(DeleteProfile);

        this.WhenActivated(HandleActivation);
    }

    private async void HandleActivation(Action<IDisposable> disposables)
    {
        var profiles = await _sessionProfileLocalDataManager.GetAllSavedProfiles();

        Profiles.Clear();
        foreach (var sessionProfile in profiles)
        {
            var profileViewModel = new ProfileViewModel(sessionProfile);
            Profiles.Add(profileViewModel);
        }

        // if (_commandLineModeHandler.IsAutoRunProfile())
        // {
        //     profileToRun = 
        // }
    }

    internal ObservableCollection<ProfileViewModel> Profiles { get; set; }

    public ReactiveCommand<ProfileViewModel, Unit> StartProfileSynchronizationCommand { get; set; }
    
    public ReactiveCommand<ProfileViewModel, Unit> StartProfileInventoryCommand { get; set; }
    
    public ReactiveCommand<ProfileViewModel, Unit> ShowProfileDetailsCommand { get; set; }
    
    public ReactiveCommand<ProfileViewModel, Unit> JoinProfileCommand { get; set; }
    
    public ReactiveCommand<ProfileViewModel, Unit> DeleteProfileCommand { get; set; }

    private async Task StartProfileSynchronization(ProfileViewModel profileViewModel)
    {
        try
        {
            await _lobbyManager.StartLobbyAsync(profileViewModel.SessionProfile, JoinLobbyModes.RunSynchronization);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "StartProfile - RunSynchronization");
        }
    }
    
    private async Task StartProfileInventory(ProfileViewModel profileViewModel)
    {
        try
        {
            await _lobbyManager.StartLobbyAsync(profileViewModel.SessionProfile, JoinLobbyModes.RunInventory);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "StartProfile - RunInventory");
        }
    }
    
    private async Task ShowProfileDetails(ProfileViewModel profileViewModel)
    {
        try
        {
            await _lobbyManager.ShowProfileDetails(profileViewModel.SessionProfile);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "StartProfile - Details");
        }
    }
    
    private async Task JoinProfile(ProfileViewModel profileViewModel)
    {
        try
        {
            await _lobbyManager.StartLobbyAsync(profileViewModel.SessionProfile, JoinLobbyModes.Join);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "StartProfile");
        }
    }
    
    private async Task DeleteProfile(ProfileViewModel profileViewModel)
    {
        try
        {
            var messageBoxViewModel = _dialogService.CreateMessageBoxViewModel(
                nameof(Resources.ProfilesView_DeleteProfile_Title), nameof(Resources.ProfilesView_DeleteProfile_Message),
                profileViewModel.Name);
            messageBoxViewModel.ShowYesNo = true;
            var result = await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);

            if (result == MessageBoxResult.Yes)
            {
                Log.Information("Deleting Session Profile {ProfileName} on user request", profileViewModel.Name);

                var isDeleted = await _profileManager.DeleteSessionProfile(profileViewModel.SessionProfile);

                if (isDeleted)
                {
                    Profiles.Remove(profileViewModel);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DeleteProfile");
        }
    }
}