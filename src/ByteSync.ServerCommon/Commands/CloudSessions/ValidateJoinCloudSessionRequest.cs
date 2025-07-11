using ByteSync.Common.Business.Sessions.Cloud.Connections;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class ValidateJoinCloudSessionRequest : IRequest<Unit>
{
    public ValidateJoinCloudSessionRequest(ValidateJoinCloudSessionParameters parameters)
    {
        Parameters = parameters;
    }
    
    public ValidateJoinCloudSessionParameters Parameters { get; }
} 