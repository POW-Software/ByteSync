using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.EventsHubs;
using MediatR;
using Serilog;

namespace ByteSync.Commands.Sessions.Connecting;

public class OnYouGaveAWrongPasswordCommandHandler : IRequestHandler<OnYouGaveAWrongPasswordRequest>
{
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ICloudSessionEventsHub _cloudSessionEventsHub;
    private readonly ILogger<OnYouGaveAWrongPasswordCommandHandler> _logger;
    
    private const string UNKNOWN_RECEIVED_SESSION_ID = "unknown received sessionId {sessionId}";

    public OnYouGaveAWrongPasswordCommandHandler(ICloudSessionConnectionRepository cloudSessionConnectionRepository, 
        ICloudSessionEventsHub cloudSessionEventsHub, ILogger<OnYouGaveAWrongPasswordCommandHandler> logger)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _cloudSessionEventsHub = cloudSessionEventsHub;
        _logger = logger;
    }
    
    public async Task Handle(OnYouGaveAWrongPasswordRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _cloudSessionConnectionRepository.CheckConnectingCloudSession(request.SessionId))
            {
                _logger.LogError(UNKNOWN_RECEIVED_SESSION_ID, request.SessionId);
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