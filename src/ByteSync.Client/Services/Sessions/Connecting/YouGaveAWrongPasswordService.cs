using System.Threading.Tasks;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using Serilog;

namespace ByteSync.Commands.Sessions.Connecting;

public class YouGaveAWrongPasswordService : IYouGaveAWrongPasswordService
{
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ICloudSessionEventsHub _cloudSessionEventsHub;
    private readonly ILogger<YouGaveAWrongPasswordService> _logger;
    
    private const string UNKNOWN_RECEIVED_SESSION_ID = "unknown received sessionId {sessionId}";

    public YouGaveAWrongPasswordService(ICloudSessionConnectionRepository cloudSessionConnectionRepository, 
        ICloudSessionEventsHub cloudSessionEventsHub, ILogger<YouGaveAWrongPasswordService> logger)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _cloudSessionEventsHub = cloudSessionEventsHub;
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

            _cloudSessionConnectionRepository.SetConnectionStatus(ConnectionStatuses.None);
            
            await _cloudSessionEventsHub.RaiseJoinCloudSessionFailed(JoinSessionResult.BuildFrom(JoinSessionStatuses.WrondPassword));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnYouGaveAWrongPassword");
        }
    }
}