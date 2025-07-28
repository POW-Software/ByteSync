using ByteSync.Common.Business.Inventories;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class GetDataSourcesRequest : IRequest<List<EncryptedDataSource>>
{
    public GetDataSourcesRequest(string sessionId, string clientInstanceId, string dataNodeId)
    {
        SessionId = sessionId;
        ClientInstanceId = clientInstanceId;
        DataNodeId = dataNodeId;
    }
    
    public string SessionId { get; }
    
    public string ClientInstanceId { get; }
    
    public string DataNodeId { get; }

}