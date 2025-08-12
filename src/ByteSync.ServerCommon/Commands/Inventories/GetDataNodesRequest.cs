using ByteSync.Common.Business.Sessions;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class GetDataNodesRequest : IRequest<List<EncryptedDataNode>>
{
    public GetDataNodesRequest(string sessionId, string clientInstanceId)
    {
        SessionId = sessionId;
        ClientInstanceId = clientInstanceId;
    }
    
    public string SessionId { get; }
    
    public string ClientInstanceId { get; }
}