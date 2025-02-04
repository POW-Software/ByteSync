using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;

namespace ByteSync.Services.Sessions.Connecting;

public class YouGaveAWrongPasswordService : IYouGaveAWrongPasswordService
{
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly ILogger<YouGaveAWrongPasswordService> _logger;
    
    private const string UNKNOWN_RECEIVED_SESSION_ID = "unknown received sessionId {sessionId}";

    public YouGaveAWrongPasswordService(ICloudSessionConnectionRepository cloudSessionConnectionRepository, 
        ICloudSessionConnector cloudSessionConnector, ILogger<YouGaveAWrongPasswordService> logger)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _cloudSessionConnector = cloudSessionConnector;
        _logger = logger;
    }
    
    public async Task Process(string sessionId)
    {
        try
        {
            if (!await _cloudSessionConnectionRepository.CheckConnectingCloudSession(sessionId))
            {
                _logger.LogError(UNKNOWN_RECEIVED_SESSION_ID, sessionId);
            }

            var joinSessionResult = JoinSessionResult.BuildFrom(JoinSessionStatus.WrongPassword);
            await _cloudSessionConnector.OnJoinSessionError(joinSessionResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnYouGaveAWrongPassword");
            
            var joinSessionResult = JoinSessionResult.BuildFrom(JoinSessionStatus.UnexpectedError);
            await _cloudSessionConnector.OnJoinSessionError(joinSessionResult);
        }
    }
}