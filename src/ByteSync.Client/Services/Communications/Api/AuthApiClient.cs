using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using ByteSync.Interfaces;
using ByteSync.Common.Business.Versions;
using System.Threading.Tasks;
using ByteSync.Common.Business.Auth;
using ByteSync.Interfaces.Controls.Communications.Http;
using Serilog;

namespace ByteSync.Services.Communications.Api;

public class AuthApiClient : IAuthApiClient
{
    private readonly IApiInvoker _apiInvoker;
    
    private readonly IApplicationSettingsRepository _settings;

    public AuthApiClient(IApiInvoker apiInvoker, IApplicationSettingsRepository settings)
    {
        _apiInvoker = apiInvoker!;
        _settings = settings;
    }
    
    public async Task<InitialAuthenticationResponse?> Login(LoginData loginData)
    {
        try
        {
            var settings = _settings.GetCurrentApplicationSettings();
            loginData.PublicKey = settings.DecodedRsaPublicKey!;
            loginData.Identity = AuthenticationProtocol.GetIdentity(loginData.PublicKey);
            loginData.AuthenticationEpoch = AuthenticationProtocol.Epoch;
            loginData.ProtocolVersion = ProtocolVersion.CURRENT;
            var challenge = await _apiInvoker.PostAsync<AuthenticationChallenge>("auth/challenge", loginData)
                ?? throw new CryptographicException("Missing authentication challenge");
            loginData.ChallengeId = challenge.Id;
            loginData.Signature = settings.PrivateRsa.SignData(AuthenticationProtocol.GetMessage(loginData, challenge),
                HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
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