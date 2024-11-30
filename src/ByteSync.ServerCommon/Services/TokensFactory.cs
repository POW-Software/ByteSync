using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Factories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ByteSync.ServerCommon.Services;

public class TokensFactory : ITokensFactory
{
    private AppSettings _appSettings;
    
    public TokensFactory(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }
    
    public JwtTokens BuildTokens(Client client)
    {
        var jwtToken = GenerateJwtToken(client);
        var refreshToken = GenerateRefreshToken(client);

        JwtTokens tokens = new JwtTokens(jwtToken, refreshToken, _appSettings.JwtDurationInSeconds);
        return tokens;
    }

    private string GenerateJwtToken(Client client)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        string role = AuthConstants.BYTESYNCUSER;

        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
            new(AuthConstants.CLAIM_IP_ADDRESS, client.IpAddress),
            new(AuthConstants.CLAIM_CLIENT_INSTANCE_ID, client.ClientInstanceId),
            new(AuthConstants.CLAIM_CLIENT_ID, client.ClientId),
            new(AuthConstants.CLAIM_VERSION, client.Version),
            new(AuthConstants.CLAIM_OS_PLATFORM, client.OsPlatform.ToString()),
        };

        var claimsIdentity = new ClaimsIdentity(claims);

        var key = Encoding.ASCII.GetBytes(_appSettings.Secret);

        var token = tokenHandler.CreateJwtSecurityToken(
            issuer: AuthConstants.ISSUER,
            audience: AuthConstants.AUDIENCE,
            subject: claimsIdentity,
            expires: DateTime.UtcNow.AddSeconds(_appSettings.JwtDurationInSeconds),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        );

        var jwtToken = tokenHandler.WriteToken(token);

        return jwtToken;
    }

    private RefreshToken GenerateRefreshToken(Client client)
    {
        var randomNumber = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            string token = Convert.ToBase64String(randomNumber);

            return new RefreshToken
            {
                Token = token,
                Expires = DateTime.UtcNow.AddSeconds(_appSettings.JwtDurationInSeconds * 2),
                Created = DateTimeOffset.UtcNow,
                CreatedByIp = client.IpAddress
            };
        }
    }
}