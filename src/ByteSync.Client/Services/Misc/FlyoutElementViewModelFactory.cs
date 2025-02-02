using Autofac;
using ByteSync.Business;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Profiles;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.AccountDetails;
using ByteSync.ViewModels.Headers;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.Profiles;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;
using ByteSync.ViewModels.Sessions.Members;
using ByteSync.ViewModels.TrustedNetworks;

namespace ByteSync.Services.Misc;

public class FlyoutElementViewModelFactory : IFlyoutElementViewModelFactory
{
    private readonly IComponentContext _context;

    public FlyoutElementViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public AddTrustedClientViewModel BuilAddTrustedClientViewModel(PublicKeyCheckData publicKeyCheckData, TrustDataParameters trustDataParameters)
    {
        var result = _context.Resolve<AddTrustedClientViewModel>(new TypedParameter(typeof(PublicKeyCheckData), publicKeyCheckData),
            new TypedParameter(typeof(TrustDataParameters), trustDataParameters));

        return result;
    }

    public SynchronizationRuleGlobalViewModel BuilSynchronizationRuleGlobalViewModel(SynchronizationRule synchronizationRule, bool isCloneMode)
    {
        var result = _context.Resolve<SynchronizationRuleGlobalViewModel>(new TypedParameter(typeof(SynchronizationRule), synchronizationRule),
            new TypedParameter(typeof(bool), isCloneMode));

        return result;
    }

    public CreateSessionProfileViewModel BuildCreateSessionProfileViewModel(ProfileTypes profileType)
    {
        var result = _context.Resolve<CreateSessionProfileViewModel>(new TypedParameter(typeof(ProfileTypes), profileType));

        return result;
    }

    public TargetedActionGlobalViewModel BuildTargetedActionGlobalViewModel(List<ComparisonItem> comparisonItems)
    {
        var result = _context.Resolve<TargetedActionGlobalViewModel>(TypedParameter.From(comparisonItems));

        return result;
    }

    public AccountDetailsViewModel BuildAccountDetailsViewModel()
    {
        var result = _context.Resolve<AccountDetailsViewModel>();

        return result;
    }

    public TrustedNetworkViewModel BuildTrustedNetworkViewModel()
    {
        var result = _context.Resolve<TrustedNetworkViewModel>();

        return result;
    }

    public UpdateDetailsViewModel BuildUpdateDetailsViewModel()
    {
        var result = _context.Resolve<UpdateDetailsViewModel>();

        return result;
    }

    public GeneralSettingsViewModel BuildGeneralSettingsViewModel()
    {
        var result = _context.Resolve<GeneralSettingsViewModel>();

        return result;
    }

    public SynchronizationRuleGlobalViewModel BuildSynchronizationRuleGlobalViewModel(SynchronizationRule? baseAutomaticAction = null, 
        bool isCloneMode = false)
    {
        var result = _context.Resolve<SynchronizationRuleGlobalViewModel>(
            new TypedParameter(typeof(SynchronizationRule), baseAutomaticAction),
            TypedParameter.From(isCloneMode));

        return result;
    }

    public FlyoutElementViewModel BuildAboutApplicationViewModel()
    {
        var result = _context.Resolve<AboutApplicationViewModel>();

        return result;
    }
}