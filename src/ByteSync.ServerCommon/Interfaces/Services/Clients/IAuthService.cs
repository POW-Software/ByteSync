using ByteSync.Common.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface IAuthService
{
    Task<InitialAuthenticationResponse> Authenticate(LoginData loginData, string ipAddress);

    Task<RefreshTokensResponse> RefreshTokens(RefreshTokensData refreshTokensData, string ipAddress);
}