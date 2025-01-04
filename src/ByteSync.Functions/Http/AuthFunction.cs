using System.Net;
using ByteSync.Common.Business.Auth;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.Http;

public class AuthFunction
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthFunction> _logger;

    public AuthFunction(IAuthService authService, ILoggerFactory loggerFactory)
    {
        _authService = authService;
        _logger = loggerFactory.CreateLogger<AuthFunction>();
    }

    [AllowAnonymous]
    [Function("Login")]
    public async Task<HttpResponseData> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post", "get", Route = "auth/login")] HttpRequestData req, 
        FunctionContext executionContext)
    {
        var response = req.CreateResponse();
        try
        {
            var loginData = await FunctionHelper.DeserializeRequestBody<LoginData>(req);
                
            var authResult  = await _authService.Authenticate(loginData, GetIpAddress(req));

            if (authResult.IsSuccess)
            {
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(authResult);
            }
            else
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                await response.WriteAsJsonAsync(authResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while logging in");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { error = "An internal server error occurred." });
        }
        
        return response;
    }
    
    [Function("RefreshTokens")]
    public async Task<HttpResponseData> RefreshTokens([HttpTrigger(AuthorizationLevel.Anonymous, "post", "get", Route = "auth/refreshTokens")] HttpRequestData req, 
        FunctionContext executionContext)
    {
        var response = req.CreateResponse();
        try
        {
            var refreshTokensData = await FunctionHelper.DeserializeRequestBody<RefreshTokensData>(req);
                
            var authResult = await _authService.RefreshTokens(refreshTokensData, GetIpAddress(req));

            if (authResult.IsSuccess)
            {
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(authResult);
            }
            else
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                await response.WriteAsJsonAsync(authResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while refreshing tokens");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { error = "An internal server error occurred." });
        }
        
        return response;
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