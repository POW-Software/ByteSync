using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Services.Sessions.Connecting;

namespace ByteSync.Services.Sessions.Connecting;

public class QuitSessionService : IQuitSessionService
{
    private readonly ISessionService _sessionService;
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly ICloudSessionConnectionService _cloudSessionConnectionService;
    private readonly ILogger<QuitSessionService> _logger;

    public QuitSessionService(ISessionService sessionService, ICloudSessionApiClient cloudSessionApiClient,
        ICloudSessionConnectionService cloudSessionConnectionService, ILogger<QuitSessionService> logger)
    {
        _sessionService = sessionService;
        _cloudSessionApiClient = cloudSessionApiClient;
        _cloudSessionConnectionService = cloudSessionConnectionService;
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
        
        await _cloudSessionConnectionService.InitializeConnection(SessionConnectionStatus.NoSession);

        if (session is CloudSession)
        {
            _logger.LogInformation("Quitted Cloud Session {CloudSession}", session.SessionId);
        }
        else
        {
            _logger.LogInformation("Quitted Local Session {CloudSession}", session.SessionId);
        }
    }
}