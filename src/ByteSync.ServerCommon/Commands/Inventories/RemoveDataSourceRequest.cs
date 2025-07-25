using ByteSync.Common.Business.Inventories;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class RemoveDataSourceRequest : IRequest<bool>
{
    public RemoveDataSourceRequest(string sessionId, Client client, string clientInstanceId, 
        string dataNodeId, EncryptedDataSource encryptedDataSource)
    {
        SessionId = sessionId;
        Client = client;
        ClientInstanceId = clientInstanceId;
        DataNodeId = dataNodeId;
        EncryptedDataSource = encryptedDataSource;
    }

    public string SessionId { get; }
    
    public Client Client { get; }
    
    public string ClientInstanceId { get; }
    
    public string DataNodeId { get; }
    
    public EncryptedDataSource EncryptedDataSource { get; }
}