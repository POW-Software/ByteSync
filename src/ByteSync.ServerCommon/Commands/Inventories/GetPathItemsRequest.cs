using ByteSync.Common.Business.Inventories;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class GetPathItemsRequest : IRequest<List<EncryptedDataSource>>
{
    public GetPathItemsRequest(string sessionId, string clientInstanceId)
    {
        SessionId = sessionId;
        ClientInstanceId = clientInstanceId;
    }
    
    public string SessionId { get; }
    
    public string ClientInstanceId { get; }

}