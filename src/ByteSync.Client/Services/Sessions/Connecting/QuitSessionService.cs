using ByteSync.Business.Navigations;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Services.Sessions.Connecting;

namespace ByteSync.Services.Sessions.Connecting;

public class QuitSessionService : IQuitSessionService
{
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly ILogger<QuitSessionService> _logger;

    public QuitSessionService(ISessionService sessionService, INavigationService navigationService, ICloudSessionApiClient cloudSessionApiClient,
        ICloudSessionConnector cloudSessionConnector, ILogger<QuitSessionService> logger)
    {
        _sessionService = sessionService;
        _navigationService = navigationService;
        _cloudSessionApiClient = cloudSessionApiClient;
        _cloudSessionConnector = cloudSessionConnector;
        _logger = logger;
    }
    
    public async Task Process()
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
                await _cloudSessionApiClient.QuitCloudSession(session.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while exiting the session");
            }
        }
        
        await _cloudSessionConnector.InitializeConnection(SessionConnectionStatus.NoSession);

        if (session is CloudSession)
        {
            _logger.LogInformation("Quitted Cloud Session {CloudSession}", session.SessionId);
        }
        else
        {
            _logger.LogInformation("Quitted Local Session {CloudSession}", session.SessionId);
        }

        //
        // _navigationService.NavigateTo(NavigationPanel.Home);
        // // _cloudSessionEventsHub.RaiseCloudSessionQuitted();
    }
}