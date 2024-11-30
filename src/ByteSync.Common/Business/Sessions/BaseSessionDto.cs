namespace ByteSync.Common.Business.Sessions;

public class BaseSessionDto
{
    public BaseSessionDto()
    {
        
    }
    
    public BaseSessionDto(string sessionId, string clientInstanceId)
    {
        SessionId = sessionId;
        ClientInstanceId = clientInstanceId;
    }
    
    public string SessionId { get; set; } = null!;

    public string ClientInstanceId { get; set; } = null!;
}