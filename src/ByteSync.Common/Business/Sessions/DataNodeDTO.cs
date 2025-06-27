namespace ByteSync.Common.Business.Sessions;

public class DataNodeDTO : BaseSessionDto
{
    public DataNodeDTO()
    {
    }

    public DataNodeDTO(string sessionId, string clientInstanceId, EncryptedDataNode encryptedDataNode)
        : base(sessionId, clientInstanceId)
    {
        EncryptedDataNode = encryptedDataNode;
    }

    public EncryptedDataNode EncryptedDataNode { get; set; } = null!;
}
