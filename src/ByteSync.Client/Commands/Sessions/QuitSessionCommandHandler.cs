using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Navigations;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Controls.Sessions;
using MediatR;

namespace ByteSync.Commands.Sessions;

public class QuitSessionCommandHandler : IRequestHandler<QuitSessionRequest>
{
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly ILogger<QuitSessionCommandHandler> _logger;

    public QuitSessionCommandHandler(ISessionService sessionService, INavigationService navigationService, ICloudSessionApiClient cloudSessionApiClient,
        ICloudSessionConnector cloudSessionConnector, ILogger<QuitSessionCommandHandler> logger)
    {
        _sessionService = sessionService;
        _navigationService = navigationService;
        _cloudSessionApiClient = cloudSessionApiClient;
        _cloudSessionConnector = cloudSessionConnector;
        _logger = logger;
    }
    
    public async Task Handle(QuitSessionRequest request, CancellationToken cancellationToken)
    {
        var session = _sessionService.CurrentSession;

        if (session == null)
        {
            _logger.LogInformation("Can not quit Session: unknown Session");
            return;
        }

        if (session is CloudSession)
        {
            try
            {
                // Ici, le but est d'essayer de quitter la session, sans bloquer l'utilisateur pour autant si cela échoue
                await _cloudSessionApiClient.QuitCloudSession(session.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durant calling Hub... continuing exit of the session");
            }
        }
        
        await _cloudSessionConnector.ClearConnectionData();
        _sessionService.ClearCloudSession();

        if (session is CloudSession)
        {
            _logger.LogInformation("Quitted Cloud Session {CloudSession}", session.SessionId);
        }
        else
        {
            _logger.LogInformation("Quitted Local Session {CloudSession}", session.SessionId);
        }

        
        _navigationService.NavigateTo(NavigationPanel.Home);
        // _cloudSessionEventsHub.RaiseCloudSessionQuitted();
    }
}