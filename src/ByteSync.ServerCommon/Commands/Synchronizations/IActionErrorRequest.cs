using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public interface IActionErrorRequest : IRequest
{
    string SessionId { get; }
    Client Client { get; }
    string? NodeId { get; }
}
