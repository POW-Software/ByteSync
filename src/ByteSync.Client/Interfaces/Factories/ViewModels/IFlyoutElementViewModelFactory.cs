using ByteSync.Business;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Profiles;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.AccountDetails;
using ByteSync.ViewModels.Headers;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.Profiles;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;
using ByteSync.ViewModels.Sessions.Members;
using ByteSync.ViewModels.TrustedNetworks;

namespace ByteSync.Interfaces.Factories.ViewModels;

public interface IFlyoutElementViewModelFactory
{
    AddTrustedClientViewModel BuilAddTrustedClientViewModel(PublicKeyCheckData publicKeyCheckData, TrustDataParameters trustDataParameters);

    SynchronizationRuleGlobalViewModel BuilSynchronizationRuleGlobalViewModel(SynchronizationRule synchronizationRule, bool isCloneMode);
    
    CreateSessionProfileViewModel BuildCreateSessionProfileViewModel(ProfileTypes profileType);
    
    TargetedActionGlobalViewModel BuildTargetedActionGlobalViewModel(List<ComparisonItem> comparisonItems);
    
    AccountDetailsViewModel BuildAccountDetailsViewModel();
    
    TrustedNetworkViewModel BuildTrustedNetworkViewModel();
    
    UpdateDetailsViewModel BuildUpdateDetailsViewModel();
    
    GeneralSettingsViewModel BuildGeneralSettingsViewModel();
    
    SynchronizationRuleGlobalViewModel BuildSynchronizationRuleGlobalViewModel(SynchronizationRule? baseAutomaticAction = null, 
        bool isCloneMode = false);

    FlyoutElementViewModel BuildAboutApplicationViewModel();
}