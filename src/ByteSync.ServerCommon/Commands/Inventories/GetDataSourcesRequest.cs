using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Security;
using ByteSync.Common.Business.Inventories;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class GetDataSourcesRequest : IRequest<List<EncryptedDataSource>>, ISessionRequest
{
    public Client? Client { get; }

    public GetDataSourcesRequest(string sessionId, string clientInstanceId, string dataNodeId, Client? client = null)
    {
        Client = client;
        SessionId = sessionId;
        ClientInstanceId = clientInstanceId;
        DataNodeId = dataNodeId;
    }
    
    public string SessionId { get; }
    
    public string ClientInstanceId { get; }
    
    public string DataNodeId { get; }

}