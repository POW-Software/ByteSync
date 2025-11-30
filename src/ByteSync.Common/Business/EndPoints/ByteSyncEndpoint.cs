using System;
using ByteSync.Common.Business.Misc;

namespace ByteSync.Common.Business.EndPoints;

public class ByteSyncEndpoint
{
    public ByteSyncEndpoint()
    {
            
    }
    
    public string ClientId { get; set; } = null!;

    public string ClientInstanceId { get; set; } = null!;
    
    public string Version { get; set; } = null!;

    public OSPlatforms OSPlatform { get; set; }

    public string IpAddress { get; set; } = null!;

    protected bool Equals(ByteSyncEndpoint other)
    {
        return ClientInstanceId == other.ClientInstanceId;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ByteSyncEndpoint)obj);
    }

    public override int GetHashCode()
    {
        return ClientInstanceId.GetHashCode();
    }
}