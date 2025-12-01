using System;

namespace ByteSync.Common.Business.EndPoints;

public class PublicKeyCheckData
{
    public PublicKeyCheckData()
    {
    }
    
    public PublicKeyInfo IssuerPublicKeyInfo { get; set; } = null!;
    
    public string IssuerClientInstanceId { get; set; } = null!;
    
    public string IssuerPublicKeyHash { get; set; }
    
    public bool? OtherPartyCheckResponse { get; set; }
    
    public string Salt { get; set; } = null!;
    
    public bool IsTrustedByOtherParty
    {
        get { return OtherPartyCheckResponse != null && OtherPartyCheckResponse.Value; }
    }
    
    public PublicKeyInfo? OtherPartyPublicKeyInfo { get; set; }
    
    public int ProtocolVersion { get; set; }
    
    protected bool Equals(PublicKeyCheckData other)
    {
        return IssuerPublicKeyInfo.Equals(other.IssuerPublicKeyInfo) && IssuerClientInstanceId == other.IssuerClientInstanceId;
    }
    
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }
        
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        
        if (obj.GetType() != this.GetType())
        {
            return false;
        }
        
        return Equals((PublicKeyCheckData)obj);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(IssuerPublicKeyInfo, IssuerClientInstanceId);
    }
}