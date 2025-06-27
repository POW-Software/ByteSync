using ByteSync.Common.Business.Inventories;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class AddDataSourceRequest : IRequest<bool>
{
    public AddDataSourceRequest(string sessionId, Client client, string nodeId, EncryptedDataSource encryptedDataSource)
    {
        SessionId = sessionId;
        Client = client;
        NodeId = nodeId;
        EncryptedDataSource = encryptedDataSource;
    }
    
    public string SessionId { get; }
    
    public Client Client { get; }

    public string NodeId { get; }
    
    public EncryptedDataSource EncryptedDataSource { get; }
}