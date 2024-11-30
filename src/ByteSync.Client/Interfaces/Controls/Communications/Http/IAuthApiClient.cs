using System.Threading.Tasks;
using ByteSync.Common.Business.Auth;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface IAuthApiClient
{
    Task<InitialAuthenticationResponse?> Login(LoginData loginData);
    
    Task<RefreshTokensResponse?> RefreshAuthenticationTokens(RefreshTokensData refreshTokensData);
}