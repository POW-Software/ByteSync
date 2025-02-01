using ByteSync.ViewModels.Profiles;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Home;

public class HomeMainViewModel : ViewModelBase, IRoutableViewModel
{
    public HomeMainViewModel()
    {
            
    }
        
    public HomeMainViewModel(IScreen screen, 
        CreateCloudSessionViewModel createCloudSessionViewModel, JoinCloudSessionViewModel joinCloudSessionViewModel)
    {
        HostScreen = screen;

        CreateCloudSession = createCloudSessionViewModel;
        JoinCloudSession = joinCloudSessionViewModel;
    }

    public string? UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);
        
    public IScreen HostScreen { get; } 
    
    [Reactive]
    public ViewModelBase CreateCloudSession { get; set; }
    
    [Reactive]
    public ViewModelBase JoinCloudSession { get; set; }
    
    [Reactive]
    internal ProfilesViewModel Profiles { get; set; }
}