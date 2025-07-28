using System.Reactive.Linq;
using System.Reactive.Subjects;
using ByteSync.Assets.Resources;
using ByteSync.Business.Navigations;
using ByteSync.Interfaces.Controls.Navigations;

namespace ByteSync.Services.Navigations;

public class NavigationService : INavigationService
{
    private BehaviorSubject<NavigationDetails> _navigationSubject;
    
    public NavigationService()
    {
        _navigationSubject = new BehaviorSubject<NavigationDetails>(new NavigationDetails());

        CurrentPanel = _navigationSubject.AsObservable();
    }
    
    public IObservable<NavigationDetails> CurrentPanel { get; }
    
    public void NavigateTo(NavigationPanel navigationPanel)
    {
        NavigationDetails navigationDetails;

        switch (navigationPanel)
        {
            case NavigationPanel.Home:
                navigationDetails = BuildNavigationInfo(navigationPanel, "RegularHomeAlt", nameof(Resources.Shell_Home));
                break;
            
            case NavigationPanel.CloudSynchronization:
                navigationDetails = BuildNavigationInfo(navigationPanel, "RegularAnalyse", nameof(Resources.OperationSelection_CloudSynchronization));
                break;
            
            case NavigationPanel.LocalSynchronization:
                navigationDetails = BuildNavigationInfo(navigationPanel, "RegularRotateLeft", nameof(Resources.OperationSelection_LocalSynchronization));
                break;
            
            case NavigationPanel.ProfileSessionLobby:
                navigationDetails = BuildNavigationInfo(navigationPanel, "RegularUnite", nameof(Resources.OperationSelection_Lobby));
                break;
            
            case NavigationPanel.ProfileSessionDetails:
                navigationDetails = BuildNavigationInfo(navigationPanel, "RegularDetail", nameof(Resources.OperationSelection_ProfileDetails));
                break;
            
            default:
                throw new Exception("Unhandled NavigationPanel");
        }
        
        _navigationSubject.OnNext(navigationDetails);
    }

    private NavigationDetails BuildNavigationInfo(NavigationPanel navigationPanel, string iconName, string titleLocalizationName)
    {
        NavigationDetails navigationDetails = new NavigationDetails
        {
            NavigationPanel = navigationPanel,
            IconName = iconName,
            TitleLocalizationName = titleLocalizationName
        };

        return navigationDetails;
    }
}