﻿using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Services.Communications.Api;

public class TrustApiClient : ITrustApiClient
{
    private readonly IApiInvoker _apiInvoker;
    private readonly ILogger<TrustApiClient> _logger;

    public TrustApiClient(IApiInvoker apiInvoker, ILogger<TrustApiClient> logger)
    {
        _apiInvoker = apiInvoker;
        _logger = logger;
    }
    
    public async Task<StartTrustCheckResult?> StartTrustCheck(TrustCheckParameters parameters)
    {
        try
        {
            return await _apiInvoker.PostAsync<StartTrustCheckResult?>($"trust/startTrustCheck", parameters);
        }
        catch (Exception ex)
        {
            LogError(ex);
                
            throw;
        }
    }

    public async Task GiveMemberPublicKeyCheckData(GiveMemberPublicKeyCheckDataParameters parameters)
    {
        try
        {
            await _apiInvoker.PostAsync($"trust/giveMemberPublicKeyCheckData", parameters);
        }
        catch (Exception ex)
        {
            LogError(ex);
                
            throw;
        }
    }

    public async Task InformPublicKeyValidationIsFinished(PublicKeyValidationParameters parameters)
    {
        try
        {
            await _apiInvoker.PostAsync($"trust/informPublicKeyValidationIsFinished", parameters);
        }
        catch (Exception ex)
        {
            LogError(ex);
                
            throw;
        }
    }

    public async Task RequestTrustPublicKey(RequestTrustProcessParameters parameters)
    {
        try
        {
            await _apiInvoker.PostAsync($"trust/requestTrustPublicKey", parameters);
        }
        catch (Exception ex)
        {
            LogError(ex);
                
            throw;
        }
    }

    public async Task SendDigitalSignatures(SendDigitalSignaturesParameters parameters)
    {
        try
        {
            await _apiInvoker.PostAsync($"trust/sendDigitalSignatures", parameters);
        }
        catch (Exception ex)
        {
            LogError(ex);
                
            throw;
        }
    }

    public async Task SetAuthChecked(SetAuthCheckedParameters parameters)
    {
        try
        {
            await _apiInvoker.PostAsync($"trust/setAuthChecked", parameters);
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
        _logger.LogError(exception, caller);
    }
}