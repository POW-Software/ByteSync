namespace ByteSync.ServerCommon.Business.Auth;

public class RefreshToken : ICloneable
{
    public RefreshToken()
    {

    }

    public string Token { get; set; }

    public DateTimeOffset Expires { get; set; }
    
    public DateTimeOffset Created { get; set; }
    
    public string CreatedByIp { get; set; }
    
    public DateTimeOffset? Revoked { get; set; }
    
    public string RevokedByIp { get; set; }
    
    public string ReplacedByToken { get; set; }

    public bool IsActive
    {
        get { return Revoked == null && !IsExpired; }
    }

    public bool IsExpired
    {
        get { return DateTime.UtcNow >= Expires; }
    }

    public bool IsRevoked
    {
        get
        {
            return Revoked != null && DateTime.UtcNow >= Revoked.Value;
        }
    }

    public object Clone()
    {
        return this.MemberwiseClone();
    }

    protected bool Equals(RefreshToken other)
    {
        return Token == other.Token;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((RefreshToken) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Token);
    }

    public void Revoke(RefreshToken newRefreshToken, string ipAddress)
    {
        Revoked = DateTimeOffset.UtcNow;
        RevokedByIp = ipAddress;
        ReplacedByToken = newRefreshToken.Token;
    }
}