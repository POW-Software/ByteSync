using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public interface IActionCompletedRequest : IRequest
{
    string SessionId { get; }
    Client Client { get; }
    List<string> ActionsGroupIds { get; }
    string? NodeId { get; }
}
