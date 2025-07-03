using System.Net;
using ByteSync.Common.Business.Auth;
using ByteSync.Functions.Helpers.Misc;
using ByteSync.ServerCommon.Commands.Authentication;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ByteSync.Functions.Http;

public class AuthFunction
{
    private readonly IMediator _mediator;

    public AuthFunction(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [Function("Login")]
    public async Task<HttpResponseData> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post", "get", Route = "auth/login")] HttpRequestData req, 
        FunctionContext executionContext)
    {
        var loginData = await FunctionHelper.DeserializeRequestBody<LoginData>(req);
        var ip = req.ExtractIpAddress();
        var authResult = await _mediator.Send(new AuthenticateCommand(loginData, ip));

        var response = req.CreateResponse();
        if (authResult.IsSuccess)
        {
            await response.WriteAsJsonAsync(authResult, HttpStatusCode.OK);
        }
        else
        {
            await response.WriteAsJsonAsync(authResult, HttpStatusCode.Unauthorized);
        }
        
        return response;
    }
    
    [Function("RefreshTokens")]
    public async Task<HttpResponseData> RefreshTokens([HttpTrigger(AuthorizationLevel.Anonymous, "post", "get", Route = "auth/refreshTokens")] HttpRequestData req, 
        FunctionContext executionContext)
    {
        var refreshTokensData = await FunctionHelper.DeserializeRequestBody<RefreshTokensData>(req);
        var ip = req.ExtractIpAddress();
        
        var authResult = await _mediator.Send(new RefreshTokensCommand(refreshTokensData, ip));
        
        var response = req.CreateResponse();
        if (authResult.IsSuccess)
        {
            await response.WriteAsJsonAsync(authResult, HttpStatusCode.OK);
        }
        else
        {
            await response.WriteAsJsonAsync(authResult, HttpStatusCode.Unauthorized);
        }
        
        return response;
    }
}