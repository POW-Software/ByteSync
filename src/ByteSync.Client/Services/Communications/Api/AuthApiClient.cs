using System.Runtime.CompilerServices;
using ByteSync.Common.Business.Auth;
using ByteSync.Interfaces.Controls.Communications.Http;
using Serilog;

namespace ByteSync.Services.Communications.Api;

public class AuthApiClient : IAuthApiClient
{
    private readonly IApiInvoker _apiInvoker;
    
    public AuthApiClient(IApiInvoker apiInvoker)
    {
        _apiInvoker = apiInvoker!;
    }
    
    public async Task<InitialAuthenticationResponse?> Login(LoginData loginData)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<InitialAuthenticationResponse>($"auth/login", loginData);
            
            return result;
        }
        catch (Exception ex)
        {
            LogError(ex);
            
            throw;
        }
    }
    
    public async Task<RefreshTokensResponse?> RefreshAuthenticationTokens(RefreshTokensData refreshTokensData)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<RefreshTokensResponse>($"auth/refreshTokens", refreshTokensData);
            
            return result;
        }
        catch (Exception ex)
        {
            LogError(ex);
            
            throw;
        }
    }
    
    private void LogError(Exception exception, [CallerMemberName] string caller = "")
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        Log.Error(exception, caller);
    }
}