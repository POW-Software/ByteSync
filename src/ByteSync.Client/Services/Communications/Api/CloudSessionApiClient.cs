using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Controls.Communications.Http;
using Serilog;

namespace ByteSync.Services.Communications.Api;

public class CloudSessionApiClient : ICloudSessionApiClient
{
    private readonly IApiInvoker _apiInvoker;
    
    public CloudSessionApiClient(IApiInvoker apiInvoker)
    {
        _apiInvoker = apiInvoker!;
    }
    
    public async Task<CloudSessionResult> CreateCloudSession(CreateCloudSessionParameters parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<CloudSessionResult>($"session", parameters, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            LogError(ex);
                
            throw;
        }
    }

    public async Task QuitCloudSession(string sessionId)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/quit");
        }
        catch (Exception ex)
        {
            LogError(ex);
                
            throw;
        }
    }
    
    public async Task ResetCloudSession(string sessionId)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/reset");
        }
        catch (Exception ex)
        {
            LogError(ex);
                
            throw;
        }
    }

    public async Task<JoinSessionResult> AskPasswordExchangeKey(AskCloudSessionPasswordExchangeKeyParameters parameters, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<JoinSessionResult>($"session/{parameters.SessionId}/askPasswordExchangeKey", parameters, 
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            LogError(ex);
                
            throw;
        }
    }

    public async Task<FinalizeJoinSessionResult> FinalizeJoinCloudSession(FinalizeJoinCloudSessionParameters parameters, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<FinalizeJoinSessionResult>($"session/{parameters.SessionId}/finalizeJoin", parameters, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            LogError(ex);
                
            throw;
        }
    }

    public async Task GiveCloudSessionPasswordExchangeKey(GiveCloudSessionPasswordExchangeKeyParameters parameters, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{parameters.SessionId}/givePassworkExchangeKey", parameters, cancellationToken);
        }
        catch (Exception ex)
        {
            LogError(ex);
                
            throw;
        }
    }

    public async Task<JoinSessionResult> AskJoinCloudSession(AskJoinCloudSessionParameters parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<JoinSessionResult>($"session/{parameters.SessionId}/askJoin", parameters, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            LogError(ex);
                
            throw;
        }
    }

    public async Task ValidateJoinCloudSession(ValidateJoinCloudSessionParameters parameters)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{parameters.SessionId}/validateJoin", parameters);
        }
        catch (Exception ex)
        {
            LogError(ex);
                
            throw;
        }
    }

    public async Task InformPasswordIsWrong(string sessionId, string joinerInstanceId)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/informPasswordIsWrong", joinerInstanceId);
        }
        catch (Exception ex)
        {
            LogError(ex);
                
            throw;
        }
    }

    public async Task UpdateSettings(string sessionId, EncryptedSessionSettings encryptedSessionSettings)
    {
        try
        {
            await _apiInvoker.PostAsync($"session/{sessionId}/updateSettings", encryptedSessionSettings);
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