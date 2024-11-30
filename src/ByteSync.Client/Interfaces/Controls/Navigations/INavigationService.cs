using ByteSync.Business.Navigations;

namespace ByteSync.Interfaces.Controls.Navigations;

public interface INavigationService
{
    IObservable<NavigationDetails> CurrentPanel { get; }
    
    void NavigateTo(NavigationPanel navigationPanel);
}