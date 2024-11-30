using System.Net;
using ByteSync.Common.Business.Auth;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post", "get", Route = "auth/login")] HttpRequestData req, 
        FunctionContext executionContext)
    {
        try
        {
            var loginData = await FunctionHelper.DeserializeRequestBody<LoginData>(req);
                
            var response = await _authService.Authenticate(loginData, GetIpAddress(req));

            if (response.IsSuccess)
            {
                return new OkObjectResult(response);
            }
            else
            {
                return new UnauthorizedObjectResult(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while logging in");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("RefreshTokens")]
    public async Task<IActionResult> RefreshTokens([HttpTrigger(AuthorizationLevel.Anonymous, "post", "get", Route = "auth/refreshTokens")] HttpRequestData req, 
        FunctionContext executionContext)
    {
        try
        {
            var refreshTokensData = await FunctionHelper.DeserializeRequestBody<RefreshTokensData>(req);
                
            var response = await _authService.RefreshTokens(refreshTokensData, GetIpAddress(req));

            if (response.IsSuccess)
            {
                return new OkObjectResult(response);
            }
            else
            {
                return new UnauthorizedObjectResult(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while refreshing tokens");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
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