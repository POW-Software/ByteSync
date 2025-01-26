using ByteSync.Common.Business.Inventories;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class GetPathItemsRequest : IRequest<List<EncryptedPathItem>>
{
    public string SessionId { get; }
    public string ClientInstanceId { get; }

    public GetPathItemsRequest(string sessionId, string clientInstanceId)
    {
        SessionId = sessionId;
        ClientInstanceId = clientInstanceId;
    }
}