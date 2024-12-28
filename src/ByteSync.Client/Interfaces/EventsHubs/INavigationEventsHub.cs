using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Events;
using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Interfaces.EventsHubs;

public interface INavigationEventsHub
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

    // void RaiseLogOutRequested();
    
    // void RaiseCloseFlyoutRequested();
    
    // void RaiseViewAccountRequested();
    
    // void RaiseViewTrustedNetworkRequested();
    
    // void RaiseViewGeneralSettingsRequested();
    
    // void RaiseViewUpdateDetailsRequested();
    
    // void RaiseNavigateToHomeRequested();
    
    // Task RaiseNavigateToCloudSynchronizationRequested();
    //
    // void RaiseNavigateToLocalSynchronizationRequested();
    
    // void RaiseNavigateToLobbyRequested();

    // Task RaiseNavigateToProfileDetailsRequested();

    // void RaiseNavigated(NavigationInfos navigationInfos);
    
    // void RaiseLogInSucceeded(ByteSyncEndpoint byteSyncEndpoint, ProductSerialDescription productSerialDescription);
    
    // void RaiseManualActionCreationRequested(ComparisonResultViewModel comparisonResultViewModel, List<ComparisonItemViewModel> comparisonItemViewModels);
    
    // void RaiseSynchronizationRuleCreationRequested();

    // void RaiseSynchronizationRuleEditionRequested(SynchronizationRule synchronizationRule);
    
    // void RaiseSynchronizationRuleDuplicationRequested(SynchronizationRule synchronizationRule);
    
    void RaiseTrustKeyDataRequested(PublicKeyCheckData publicKeyCheckData, TrustDataParameters trustDataParameters);
    
    Task RaiseCreateCloudSessionProfileRequested();

    Task RaiseCreateLocalSessionProfileRequested();
}