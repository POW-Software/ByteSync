using ByteSync.Business.Sessions.Connecting;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ByteSync.Interfaces.Services.Sessions.Connecting.Joining;

namespace ByteSync.Services.Sessions.Connecting;

public class YouGaveAWrongPasswordService : IYouGaveAWrongPasswordService
{
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ICloudSessionConnectionService _cloudSessionConnectionService;
    private readonly ILogger<YouGaveAWrongPasswordService> _logger;
    
    private const string UNKNOWN_RECEIVED_SESSION_ID = "unknown received sessionId {sessionId}";

    public YouGaveAWrongPasswordService(ICloudSessionConnectionRepository cloudSessionConnectionRepository, 
        ICloudSessionConnectionService cloudSessionConnectionService, ILogger<YouGaveAWrongPasswordService> logger)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _cloudSessionConnectionService = cloudSessionConnectionService;
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

            var joinSessionError = new JoinSessionError
            {
                Status = JoinSessionStatus.WrongPassword
            };
            
            await _cloudSessionConnectionService.OnJoinSessionError(joinSessionError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnYouGaveAWrongPassword");
            
            var joinSessionError = new JoinSessionError
            {
                Exception = ex,
                Status = JoinSessionStatus.UnexpectedError
            };
            
            await _cloudSessionConnectionService.OnJoinSessionError(joinSessionError);
        }
    }
}