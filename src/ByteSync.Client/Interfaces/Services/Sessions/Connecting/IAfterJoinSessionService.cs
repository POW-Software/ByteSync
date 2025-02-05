using ByteSync.Business.Sessions.Connecting;

namespace ByteSync.Interfaces.Services.Sessions.Connecting;

public interface IAfterJoinSessionService
{
    Task Process(AfterJoinSessionRequest request);
}