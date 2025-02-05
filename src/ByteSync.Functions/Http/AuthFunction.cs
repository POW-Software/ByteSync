using System.Net;
using ByteSync.Common.Business.Auth;
using ByteSync.Functions.Constants;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.Http;

public class AuthFunction
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthFunction> _logger;
    private readonly TelemetryClient _telemetryClient;

    public AuthFunction(IAuthService authService, ILoggerFactory loggerFactory, TelemetryClient telemetryClient)
    {
        _authService = authService;
        _logger = loggerFactory.CreateLogger<AuthFunction>();
        _telemetryClient = telemetryClient;
    }

    [AllowAnonymous]
    [Function("Login")]
    public async Task<HttpResponseData> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post", "get", Route = "auth/login")] HttpRequestData req, 
        FunctionContext executionContext)
    {
        using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("Login"))
        {
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["OperationId"] = operation.Telemetry.Context.Operation.Id
            }))
            {
                var response = req.CreateResponse();
                try
                {
                    var loginData = await FunctionHelper.DeserializeRequestBody<LoginData>(req);

                    var authResult = await _authService.Authenticate(loginData, GetIpAddress(req));

                    if (authResult.IsSuccess)
                    {
                        await response.WriteAsJsonAsync(authResult, HttpStatusCode.OK);
                    }
                    else
                    {
                        await response.WriteAsJsonAsync(authResult, HttpStatusCode.Unauthorized);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while logging in");
                    await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
                }

                return response;
            }
        }
    }
    
    [Function("RefreshTokens")]
    public async Task<HttpResponseData> RefreshTokens([HttpTrigger(AuthorizationLevel.Anonymous, "post", "get", Route = "auth/refreshTokens")] HttpRequestData req, 
        FunctionContext executionContext)
    {
        using (_logger.BeginScope("RefreshTokens"))
        {
            var response = req.CreateResponse();
            try
            {
                var refreshTokensData = await FunctionHelper.DeserializeRequestBody<RefreshTokensData>(req);
                
                var authResult = await _authService.RefreshTokens(refreshTokensData, GetIpAddress(req));

                if (authResult.IsSuccess)
                {
                    await response.WriteAsJsonAsync(authResult, HttpStatusCode.OK);
                }
                else
                {
                    await response.WriteAsJsonAsync(authResult, HttpStatusCode.Unauthorized);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while refreshing tokens");
                await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
            }
        
            return response;
        }
    }

    private string GetIpAddress(HttpRequestData req)
    {
        var headerDictionary = req.Headers.ToDictionary(x => x.Key.ToLower(), x => x.Value, StringComparer.Ordinal);
        var key = "x-forwarded-for";
        if (headerDictionary.ContainsKey(key))
        {
            IPAddress? ipAddress = null;
            var headerValues = headerDictionary[key];
            var ipn = headerValues?.FirstOrDefault()?.Split(new char[] { ',' }).FirstOrDefault()?.Split(new char[] { ':' }).FirstOrDefault();
            if (IPAddress.TryParse(ipn, out ipAddress))
            {
                var ipAddressString = ipAddress.ToString();
                return ipAddressString;
            }
        }

        return "";
    }
}