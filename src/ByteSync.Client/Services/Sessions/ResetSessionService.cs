using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Local;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Sessions;

namespace ByteSync.Services.Sessions;

public class ResetSessionService : IResetSessionService
{
    private readonly ISessionService _sessionService;
    private readonly ISessionMemberService _sessionMemberService;
    private readonly ICloudProxy _connectionManager;
    private readonly ICloudSessionLocalDataManager _cloudSessionLocalDataManager;
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly ILogger<ResetSessionService> _logger;

    public ResetSessionService(ISessionService sessionService, ISessionMemberService sessionMemberService, ICloudProxy connectionManager, 
        ICloudSessionLocalDataManager cloudSessionLocalDataManager, ICloudSessionApiClient cloudSessionApiClient, 
        ILogger<ResetSessionService> logger)
    {
        _sessionService = sessionService;
        _sessionMemberService = sessionMemberService;
        _connectionManager = connectionManager;
        _cloudSessionLocalDataManager = cloudSessionLocalDataManager;
        _cloudSessionApiClient = cloudSessionApiClient;
        _logger = logger;
        
        _connectionManager.HubPushHandler2.SessionResetted
            .Subscribe(dto =>
            {
                if (dto.SessionId.Equals(_sessionService.SessionId))
                {
                    _ = HandleReset();
                }
                else
                {
                    // sessionId is not expected, how to deal with that?
                }
            });
    }
    
    public async Task ResetSession()
    {
        var session = _sessionService.CurrentSession;

        if (session != null)
        {
            _logger.LogInformation("Restarting session {SessionId}", session.SessionId);
            
            try
            {
                if (session is CloudSession)
                {
                    await _cloudSessionApiClient.ResetCloudSession(session.SessionId);
                    
                    await HandleReset();
                }
                else if (session is LocalSession)
                {
                    await HandleReset();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SessionResetter.ResetSession");
            }
        }
        else
        {
            _logger.LogError("SessionResetter.ResetSession: unknown session");
        }
    }

    private async Task HandleReset()
    {
        await _cloudSessionLocalDataManager.BackupCurrentSessionFiles();

        await _sessionService.ResetSession();

        await _sessionMemberService.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.InventoryWaitingForStart);
    }
}