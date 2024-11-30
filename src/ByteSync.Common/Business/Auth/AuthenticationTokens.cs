using System;

namespace ByteSync.Common.Business.Auth;

public class AuthenticationTokens : ICloneable
{
    public string JwtToken { get; set; } = null!;

    public int JwtTokenDurationInSeconds { get; set; }

    public string RefreshToken { get; set; } = null!;

    public DateTimeOffset RefreshTokenExpiration { get; set; }

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}