using System.Threading.Tasks;
using ByteSync.Commands.Sessions;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Controls.Sessions;
using MediatR;
using ReactiveUI;
using Serilog;
using Unit = System.Reactive.Unit;

namespace ByteSync.ViewModels.Home;

public class CreateCloudSessionViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly IMediator _mediator;

    public CreateCloudSessionViewModel()
    {
    }

    public CreateCloudSessionViewModel(INavigationService navigationService, 
        ICloudSessionConnector cloudSessionConnector, IMediator mediator)
    {
        _navigationService = navigationService;
        _cloudSessionConnector = cloudSessionConnector;
        _mediator = mediator;
        
        StartComparisonCommand = ReactiveCommand.CreateFromTask(CreateSession);
        
        // CancelCommand = ReactiveCommand.Create(() => _navigationService.NavigateTo(NavigationPanel.Home));
    }

    public ReactiveCommand<Unit, Unit>? StartComparisonCommand { get; set; }
    
    private async Task CreateSession()
    {
        // CloudSessionManagement = StartCloudSessionViewModel;
        // await StartCloudSessionViewModel!.CreateSession();
        
        try
        {
            await _mediator.Send(new CreateSessionRequest(null));
        }
        catch (Exception ex)
        {
            // IsProgressActive = false;
            // IsError = true;
            Log.Error(ex, "CreateSession");
        }
    }
}