using System.Threading.Tasks;
using ByteSync.Common.Business.Auth;

namespace ByteSync.Interfaces.Services.Communications;

public interface IAuthenticationTokensRepository
{
    Task<AuthenticationTokens?> GetTokens();
    
    Task Store(AuthenticationTokens authenticationTokens);
}