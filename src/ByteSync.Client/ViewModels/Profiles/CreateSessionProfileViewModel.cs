using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Avalonia.Controls;
using ByteSync.Business.Profiles;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Profiles;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.ViewModels.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.Profiles;

public class CreateSessionProfileViewModel : FlyoutElementViewModel
{
    private readonly ISessionService _sessionService;
    private readonly ISessionProfileManager _sessionProfileManager;
    private readonly IDialogService _dialogService;
    private readonly ISessionProfileLocalDataManager _sessionProfileLocalDataManager;

    public CreateSessionProfileViewModel()
    {
    #if DEBUG
        if (Design.IsDesignMode)
        {

        }
    #endif
    }
    
    public CreateSessionProfileViewModel(ProfileTypes profileType, ISessionService sessionService, 
        ISessionProfileManager sessionProfileManager, IDialogService dialogService,
        ISessionProfileLocalDataManager sessionProfileLocalDataManager, ErrorViewModel errorViewModel)
    {
        _sessionService = sessionService;
        _sessionProfileManager = sessionProfileManager;
        _dialogService = dialogService;
        _sessionProfileLocalDataManager = sessionProfileLocalDataManager;

        ProfileName = "";
        ProfileType = profileType;
        
        var canResetOrCancel = new BehaviorSubject<bool>(true);
        
        var canSave = this
            .WhenAnyValue(x => x.ProfileName, profileName => profileName.IsNotEmpty()) 
            .ObserveOn(RxApp.MainThreadScheduler);
        
        SaveCommand = ReactiveCommand.CreateFromTask(Save, canSave);
        ResetCommand = ReactiveCommand.Create(Reset, canResetOrCancel);
        CancelCommand = ReactiveCommand.Create(Cancel, canResetOrCancel);
        
        Observable.Merge(SaveCommand.IsExecuting, ResetCommand.IsExecuting, CancelCommand.IsExecuting)
            .Select(executing => !executing).Subscribe(canResetOrCancel);

        ShowSuccess = false;
        IsCreatingProfile = false;

        SuggestedItems = new ObservableCollection<string>();

        Error = errorViewModel;
        
        this.WhenActivated(HandleActivation);
    }
    
    private async void HandleActivation(Action<IDisposable> disposables)
    {
        try
        {
            var currentProfilesNames = await GetCurrentProfilesNames();

            foreach (var name in currentProfilesNames)
            {
                SuggestedItems.Add(name);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "HandleActivation");
        }
    }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; set; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; set; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }
    
    [Reactive]
    internal string ProfileName { get; set; }
    
    [Reactive]
    internal ProfileTypes ProfileType { get; set; }

    [Reactive]
    public bool ShowSuccess { get; set; }
    
    [Reactive]
    public bool IsCreatingProfile { get; set; }
    
    [Reactive]
    public bool ShowWarning { get; set; }
    
    [Reactive]
    public ErrorViewModel Error { get; set; }

    public ObservableCollection<string> SuggestedItems { get; }
    

    private async Task Save()
    {
        try
        {
            if (!ShowWarning)
            {
                var currentProfilesNames = await GetCurrentProfilesNames();
                if (currentProfilesNames.Any(n => n.Equals(ProfileName.Trim())))
                {
                    ShowWarning = true;
                    return;
                }
            }

            Container.CanCloseCurrentFlyout = false;
            
            ShowSuccess = false;
            Error.Clear();
            
            IsCreatingProfile = true;

            if (ProfileType == ProfileTypes.Cloud)
            {
                var sessionId = _sessionService.SessionId;
                var cloudSessionProfileOptions = new CloudSessionProfileOptions();
                cloudSessionProfileOptions.Settings = _sessionService.CurrentSessionSettings!;
                await _sessionProfileManager.CreateCloudSessionProfile(sessionId, ProfileName, cloudSessionProfileOptions);
            }
            else
            {
                var sessionId = _sessionService.SessionId;
                var localSessionProfileOptions = new LocalSessionProfileOptions();
                localSessionProfileOptions.Settings = _sessionService.CurrentSessionSettings!;
                await _sessionProfileManager.CreateLocalSessionProfile(sessionId, ProfileName, localSessionProfileOptions);
            }

            IsCreatingProfile = false;

            ShowSuccess = true;
            await Task.Delay(TimeSpan.FromSeconds(3.5));
            ShowSuccess = false;

            _dialogService.CloseFlyout();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "QuitSession error");

            Error.SetException(ex);
        }
        finally
        {
            IsCreatingProfile = false;
            Container.CanCloseCurrentFlyout = true;
        }
    }
    
    private async Task<List<string>> GetCurrentProfilesNames()
    {
        var profiles = await _sessionProfileLocalDataManager.GetAllSavedProfiles();
        var profilesNames = Enumerable.ToHashSet(profiles.Select(p => p.Name).ToList()).ToList();
        profilesNames.Sort();
        return profilesNames;
    }

    private void Reset()
    {
        ProfileName = "";
        ShowWarning = false;
    }

    private void Cancel()
    {
        _dialogService.CloseFlyout();
    }
}