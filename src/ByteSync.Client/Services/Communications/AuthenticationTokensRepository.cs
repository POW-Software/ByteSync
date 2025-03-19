using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.Auth;
using ByteSync.Interfaces.Services.Communications;

namespace ByteSync.Services.Communications 
{
    public class AuthenticationTokensRepository : IAuthenticationTokensRepository
    {
        private AuthenticationTokens? _tokens;
        
        private readonly SemaphoreSlim _semaphore = new (1, 1);

        public async Task<AuthenticationTokens?> GetTokens()
        {
            await _semaphore.WaitAsync();
            try
            {
                return _tokens?.Clone() as AuthenticationTokens;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        public async Task Store(AuthenticationTokens authenticationTokens)
        {
            var clone = authenticationTokens.Clone() as AuthenticationTokens;
            
            await _semaphore.WaitAsync();
            try
            {
                _tokens = clone;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
