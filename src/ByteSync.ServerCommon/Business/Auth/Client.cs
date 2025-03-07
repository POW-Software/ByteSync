using ByteSync.Common.Business.Misc;

namespace ByteSync.ServerCommon.Business.Auth;

public enum ClientStatuses
{
    Created = 1,
    Connected = 2,
    Disconnected = 3,
}

public class Client
{
    public Client()
    {

    }
    
    public Client(string clientId, string clientInstanceId, string version, OSPlatforms osPlatform, string ipAddress)
    {
        ClientId = clientId;
        ClientInstanceId = clientInstanceId;
        Version = version;
        IpAddress = ipAddress;

        CreatedOn = DateTime.UtcNow;

        Status = ClientStatuses.Created;

        OsPlatform = osPlatform;
    }

    public string ClientId { get; set; } = null!;

    public string ClientInstanceId  { get; set; } = null!;
    
    public List<string> ConnectionIds { get; set; } = new ();

    public string Version { get; set; } = null!;

    public string IpAddress { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public OSPlatforms OsPlatform { get; set; }

    public ClientStatuses Status  { get; set; }

    public DateTimeOffset? ConnectedToHubOn { get; set; }

    public DateTimeOffset? LastConnectionLostOn { get; set; }

    public DateTimeOffset? DisconnectedOn { get; set; }
        
    public DateTimeOffset? InvalidSerialOn { get; set; }

    public bool HasOpenConnection
    {
        get
        {
            return ConnectionIds.Count > 0;
        }
    }

    public RefreshToken? RefreshToken { get; set; }

    public List<string> SubscribedGroups { get; set; } = new();

    protected bool Equals(Client other)
    {
        return Equals(ClientInstanceId, other.ClientInstanceId);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Client) obj);
    }

    public override int GetHashCode()
    {
        return ClientInstanceId.GetHashCode();
    }
        
    public void SetConnected(string? connectionId)
    {
        if (connectionId == null)
        {
            throw new ArgumentNullException(nameof(connectionId), "connectionId can not be null");
        }
            
        ConnectionIds.Add(connectionId);

        Status = ClientStatuses.Connected;
        ConnectedToHubOn = DateTimeOffset.UtcNow;
    }
        
    public void OnConnectionLost(string? connectionId)
    {
        if (connectionId == null)
        {
            throw new ArgumentNullException(nameof(connectionId), "connectionId can not be null");
        }
        
        ConnectionIds.Remove(connectionId);
        LastConnectionLostOn = DateTimeOffset.UtcNow;
    }
        
    public void SetDisconnected()
    {
        Status = ClientStatuses.Disconnected;
        DisconnectedOn = DateTimeOffset.UtcNow;
    }
}