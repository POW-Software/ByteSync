using System.Reactive;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.ViewModels.Profiles;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Home;

public class HomeMainViewModel : ViewModelBase, IRoutableViewModel
{
    private IWebAccessor _webAccessor;
    private readonly ISessionService _sessionService;

    public HomeMainViewModel()
    {
            
    }
        
    public HomeMainViewModel(IScreen screen, IWebAccessor webAccessor, ISessionService sessionService,
        ProfilesViewModel profilesViewModel)
    {
        HostScreen = screen;

        _webAccessor = webAccessor;
        _sessionService = sessionService;

        CloudSynchronizationCommand = ReactiveCommand.CreateFromTask(() =>
            _sessionService.InitiateCloudSessionMode());
        
        LocalSynchronizationCommand = ReactiveCommand.CreateFromTask(() =>
            _sessionService.InitiateLocalSessionMode());

        OpenSupportCommand = ReactiveCommand.CreateFromTask(() => 
            _webAccessor.OpenDocumentationUrl());

        Profiles = profilesViewModel;
    }

    public string? UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);
        
    public IScreen HostScreen { get; } 

    public ReactiveCommand<Unit, Unit> CloudSynchronizationCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> LocalSynchronizationCommand { get; set; }

    public ReactiveCommand<Unit, Unit> OpenSupportCommand { get; set; }
    
    [Reactive]
    internal ProfilesViewModel Profiles { get; set; }
}