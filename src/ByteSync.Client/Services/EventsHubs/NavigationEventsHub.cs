using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Events;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Interfaces.EventsHubs;

namespace ByteSync.Services.EventsHubs;

public class NavigationEventsHub : INavigationEventsHub
{
    // public event EventHandler<EventArgs>? LogOutRequested;
    
    // public event EventHandler<EventArgs>? CloseFlyoutRequested;
    
    // public event EventHandler<EventArgs>? ViewAccountRequested;
    //
    // public event EventHandler<EventArgs>? ViewTrustedNetworkRequested;
    
    // public event EventHandler<EventArgs>? ViewGeneralSettingsRequested;
    
    // public event EventHandler<EventArgs>? ViewUpdateDetailsRequested;
    
    // public event EventHandler<EventArgs>? NavigateToHomeRequested;
    
    // public event EventHandler<EventArgs>? NavigateToCloudSynchronizationRequested;
    //
    // public event EventHandler<EventArgs>? NavigateToLocalSynchronizationRequested;
    
    // public event EventHandler<EventArgs>? NavigateToLobbyRequested;
    
    // public event EventHandler<EventArgs>? NavigateToProfileDetailsRequested;

    // public event EventHandler<NavigatedEventArgs>? Navigated;
    
    // public event EventHandler<LogInSucceededEventArgs>? LogInSucceeded;
    
    // public event EventHandler<ManualActionCreationRequestedArgs>? ManualActionCreationRequested;
    
    // public event EventHandler<EventArgs>? SynchronizationRuleCreationRequested;

    // public event EventHandler<GenericEventArgs<SynchronizationRule>>? SynchronizationRuleEditionRequested;
    
    // public event EventHandler<GenericEventArgs<SynchronizationRule>>? SynchronizationRuleDuplicationRequested;
    
    public event EventHandler<TrustKeyDataRequestedArgs>? TrustKeyDataRequested;

    public event EventHandler<EventArgs>? CreateCloudSessionProfileRequested;
    
    public event EventHandler<EventArgs>? CreateLocalSessionProfileRequested;

    // public void RaiseLogOutRequested()
    // {
    //     Task.Run(() => LogOutRequested?.Invoke(this, EventArgs.Empty));
    // }

    // public void RaiseCloseFlyoutRequested()
    // {
    //     Task.Run(() => CloseFlyoutRequested?.Invoke(this, EventArgs.Empty));
    // }

    // public void RaiseViewAccountRequested()
    // {
    //     Task.Run(() => ViewAccountRequested?.Invoke(this, EventArgs.Empty));
    // }
    //
    // public void RaiseViewTrustedNetworkRequested()
    // {
    //     Task.Run(() => ViewTrustedNetworkRequested?.Invoke(this, EventArgs.Empty));
    // }

    // public void RaiseViewGeneralSettingsRequested()
    // {
    //     Task.Run(() => ViewGeneralSettingsRequested?.Invoke(this, EventArgs.Empty));
    // }

    // public void RaiseViewUpdateDetailsRequested()
    // {
    //     Task.Run(() => ViewUpdateDetailsRequested?.Invoke(this, EventArgs.Empty));
    // }

    // public void RaiseNavigateToHomeRequested()
    // {
    //     Task.Run(() => NavigateToHomeRequested?.Invoke(this, EventArgs.Empty));
    // }
    //
    // public async Task RaiseNavigateToCloudSynchronizationRequested()
    // {
    //     await Task.Run(() => NavigateToCloudSynchronizationRequested?.Invoke(this, EventArgs.Empty));
    //     
    //     NavigationInfos navigationInfos = new NavigationInfos
    //     { IconName = "RegularAnalyse", 
    //         TitleLocalizationName = nameof(Resources.OperationSelection_CloudSynchronization), 
    //         IsHome = false };
    //     
    //     RaiseNavigated(navigationInfos);
    // }
    //
    // public void RaiseNavigateToLocalSynchronizationRequested()
    // {
    //     Task.Run(() => NavigateToLocalSynchronizationRequested?.Invoke(this, EventArgs.Empty));
    //     
    //     NavigationInfos navigationInfos = new NavigationInfos
    //     { IconName = "RegularRotateLeft", 
    //         TitleLocalizationName = nameof(Resources.OperationSelection_LocalSynchronization), 
    //         IsHome = false };
    //     
    //     RaiseNavigated(navigationInfos);
    // }

    // public void RaiseNavigateToLobbyRequested()
    // {
    //     Task.Run(() => NavigateToLobbyRequested?.Invoke(this, EventArgs.Empty));
    //     
    //     NavigationInfos navigationInfos = new NavigationInfos
    //     { IconName = "RegularUnite", 
    //         TitleLocalizationName = nameof(Resources.OperationSelection_Lobby), 
    //         IsHome = false };
    //     
    //     RaiseNavigated(navigationInfos);
    // }
    
    // public async Task RaiseNavigateToProfileDetailsRequested()
    // {
    //     await Task.Run(() => NavigateToProfileDetailsRequested?.Invoke(this, EventArgs.Empty));
    //     
    //     NavigationInfos navigationInfos = new NavigationInfos
    //     { IconName = "RegularDetail", 
    //         TitleLocalizationName = nameof(Resources.OperationSelection_ProfileDetails), 
    //         IsHome = false };
    //     
    //     RaiseNavigated(navigationInfos);
    // }
    
    // public void RaiseNavigated(NavigationInfos navigationInfos)
    // {
    //     Task.Run(() => Navigated?.Invoke(this, new NavigatedEventArgs(navigationInfos)));
    // }

    // public void RaiseLogInSucceeded(ByteSyncEndpoint byteSyncEndpoint, ProductSerialDescription productSerialDescription)
    // {
    //     Task.Run(() => LogInSucceeded?.Invoke(this, new LogInSucceededEventArgs(byteSyncEndpoint, productSerialDescription)));
    // }

    // public void RaiseManualActionCreationRequested(ComparisonResultViewModel comparisonResultViewModel, 
    //     List<ComparisonItemViewModel> comparisonItemViewModels)
    // {
    //     Task.Run(() => ManualActionCreationRequested?.Invoke(this, 
    //         new ManualActionCreationRequestedArgs(comparisonResultViewModel, comparisonItemViewModels)));
    // }

    // public void RaiseSynchronizationRuleCreationRequested()
    // {
    //     Task.Run(() => SynchronizationRuleCreationRequested?.Invoke(this, EventArgs.Empty));
    // }

    // public void RaiseSynchronizationRuleEditionRequested(SynchronizationRule synchronizationRule)
    // {
    //     Task.Run(() => SynchronizationRuleEditionRequested?.Invoke(this, new GenericEventArgs<SynchronizationRule>(synchronizationRule)));
    // }

    // public void RaiseSynchronizationRuleDuplicationRequested(SynchronizationRule synchronizationRule)
    // {
    //     Task.Run(() => SynchronizationRuleDuplicationRequested?.Invoke(this, new GenericEventArgs<SynchronizationRule>(synchronizationRule)));
    // }

    public void RaiseTrustKeyDataRequested(PublicKeyCheckData publicKeyCheckData, TrustDataParameters trustDataParameters)
    {
        Task.Run(() => TrustKeyDataRequested?.Invoke(this, new TrustKeyDataRequestedArgs(publicKeyCheckData, trustDataParameters)));
    }

    public async Task RaiseCreateCloudSessionProfileRequested()
    {
        await Task.Run(() => CreateCloudSessionProfileRequested?.Invoke(this, EventArgs.Empty));
    }

    public async Task RaiseCreateLocalSessionProfileRequested()
    {
        await Task.Run(() => CreateLocalSessionProfileRequested?.Invoke(this, EventArgs.Empty));
    }
}