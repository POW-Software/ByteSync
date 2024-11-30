using ByteSync.Common.Business.Auth;

namespace ByteSync.ServerCommon.Business.Auth;

public class JwtTokens
{
    public JwtTokens(string jwtToken, RefreshToken refreshToken, int jwtTokenDurationInSeconds)
    {
        JwtToken = jwtToken;
        RefreshToken = refreshToken;
        JwtTokenDurationInSeconds = jwtTokenDurationInSeconds;
    }
    
    public string JwtToken { get; }
    
    public RefreshToken RefreshToken { get; }
    
    public int JwtTokenDurationInSeconds { get; }

    public AuthenticationTokens BuildAuthenticationTokens()
    {
        var authenticationTokens = new AuthenticationTokens();
        
        authenticationTokens.JwtToken = JwtToken;
        authenticationTokens.JwtTokenDurationInSeconds = JwtTokenDurationInSeconds;
        authenticationTokens.RefreshToken = RefreshToken.Token;
        authenticationTokens.RefreshTokenExpiration = RefreshToken.Expires;

        return authenticationTokens;
    }
}