using System.Collections.ObjectModel;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business.Profiles;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Profiles;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Lobbies;
using ByteSync.Interfaces.Profiles;
using ByteSync.ViewModels.Lobbies;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions;

public class ImportRulesFromProfileViewModel : ActivableViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly ISessionProfileLocalDataManager _sessionProfileLocalDataManager;
    private readonly ISynchronizationRulesConverter _synchronizationRulesConverter;
    private readonly ILobbySynchronizationRuleViewModelFactory _lobbySynchronizationRuleViewModelFactory;

    public ImportRulesFromProfileViewModel()
    {
    }

    internal ImportRulesFromProfileViewModel(ISessionService sessionService, ISessionProfileLocalDataManager sessionProfileLocalDataManager, 
        ISynchronizationRulesConverter synchronizationRulesConverter, ILobbySynchronizationRuleViewModelFactory lobbySynchronizationRuleViewModelFactory)
    {
        _sessionService = sessionService;
        _sessionProfileLocalDataManager = sessionProfileLocalDataManager;
        _synchronizationRulesConverter = synchronizationRulesConverter;
        _lobbySynchronizationRuleViewModelFactory = lobbySynchronizationRuleViewModelFactory;

        AvailableSessionProfiles = new ObservableCollection<AbstractSessionProfile>();
        CloudSessionProfileSynchronizationRules = new ObservableCollection<LobbySynchronizationRuleViewModel>();

        Error = new ErrorViewModel();
        
        this.WhenAnyValue(x => x.SelectedSessionProfile)
            .Where(x => x != null)
            .Subscribe(_ => Import());
        
        this.WhenActivated(HandleActivation);
    }

    public ObservableCollection<AbstractSessionProfile> AvailableSessionProfiles { get; set; }
    
    [Reactive]
    public AbstractSessionProfile? SelectedSessionProfile { get; set; }
    
    [Reactive]
    public List<SynchronizationRuleSummaryViewModel>? SynchronizationRuleViewModels { get; set; }
    
    [Reactive]
    public ObservableCollection<LobbySynchronizationRuleViewModel> CloudSessionProfileSynchronizationRules { get; set; }
    
    [Reactive]
    public bool IsLoading { get; set; }
    
    [Reactive]
    public ErrorViewModel Error { get; set; }
    
    private async void HandleActivation(Action<IDisposable> disposables)
    {
        var savedProfiles = await _sessionProfileLocalDataManager.GetAllSavedProfiles();

        List<AbstractSessionProfile> applicableProfiles;
        if (_sessionService.SessionObservable is CloudSession)
        {
            applicableProfiles = savedProfiles.Where(sp => sp is CloudSessionProfile).ToList();
        }
        else
        {
            applicableProfiles = savedProfiles.Where(sp => sp is LocalSessionProfile).ToList();
        }

        AvailableSessionProfiles.AddAll(applicableProfiles);
    }
    
    private async void Import()
    {
        try
        {
            AbstrastSessionProfileDetails? profileDetails;
            Error.Clear();
            SynchronizationRuleViewModels = null;
            CloudSessionProfileSynchronizationRules.Clear();

            if (SelectedSessionProfile is CloudSessionProfile cloudSessionProfile)
            {
                IsLoading = true;

                profileDetails =
                    await _sessionProfileLocalDataManager.LoadCloudSessionProfileDetails(cloudSessionProfile);
            }
            else if (SelectedSessionProfile is LocalSessionProfile localSessionProfile)
            {
                profileDetails =
                    await _sessionProfileLocalDataManager.LoadLocalSessionProfileDetails(localSessionProfile);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(SelectedSessionProfile));
            }
            
            if (profileDetails == null)
            {
                SynchronizationRuleViewModels = null;

                Error.ErrorMessageKey = nameof(Resources.ImportRulesFromProfileView_CanNotLoadProfileDetails);
                Log.Error("Can not load details for Profile {ProfileName} (Id: {ProfileId}). An element may be missing", 
                    SelectedSessionProfile.Name, SelectedSessionProfile.ProfileId);
                    
                return;
            }

            foreach (var cloudSessionProfileSynchronizationRule in profileDetails.SynchronizationRules)
            {
                var lobbySynchronizationRuleViewModel = _lobbySynchronizationRuleViewModelFactory.Create(cloudSessionProfileSynchronizationRule, true);
                
                CloudSessionProfileSynchronizationRules.Add(lobbySynchronizationRuleViewModel);
            }
            
            if (!_synchronizationRulesConverter.CheckAllDataPartsAreMappable(profileDetails.SynchronizationRules))
            {
                SynchronizationRuleViewModels = null;

                Error.ErrorMessageKey = nameof(Resources.ImportRulesFromProfileView_CanNotApplySynchronizationRules);
                Log.Error("Can not apply Synchronization Rules from Profile {ProfileName} (Id: {ProfileId}). DataSources can not be mapped", 
                    SelectedSessionProfile.Name, SelectedSessionProfile.ProfileId);
                    
                return;
            }
            
            SynchronizationRuleViewModels = _synchronizationRulesConverter.ConvertToSynchronizationRuleViewModels(profileDetails.SynchronizationRules);
        }
        catch (Exception ex)
        {
            Error.SetException(ex);

            Log.Error(ex, "Import");

            SynchronizationRuleViewModels = null;
        }
        finally
        {
            IsLoading = false;
        }
    }
}