using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface ICloudSessionsRepository : IRepository<CloudSessionData>
{
    public Task<SessionMemberData?> GetSessionMember(string sessionId, Client client);
    
    public Task<SessionMemberData?> GetSessionMember(string sessionId, string clientInstanceId);
    
    public Task<SessionMemberData?> GetSessionPreMember(string sessionId, string clientInstanceId);

    public Task<CloudSessionData> AddCloudSession(CloudSessionData cloudSessionData, Func<string> generateSessionIdHandler);
}