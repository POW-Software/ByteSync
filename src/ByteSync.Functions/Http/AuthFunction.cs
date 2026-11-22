using System.Net;
using Microsoft.Extensions.DependencyInjection;
using ByteSync.Common.Business.Auth;
using ByteSync.Functions.Helpers.Misc;
using ByteSync.ServerCommon.Commands.Authentication;
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
    [Function("AuthenticationChallenge")]
    public async Task<HttpResponseData> Challenge(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/challenge")] HttpRequestData req,
        FunctionContext context)
    {
        var login = await FunctionHelper.DeserializeRequestBody<LoginData>(req);
        var service = context.InstanceServices.GetRequiredService<ByteSync.ServerCommon.Interfaces.Services.IAuthenticationChallengeService>();
        var challenge = await service.Create(login);
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(challenge);
        return response;
    }

    [AllowAnonymous]
    [Function("Login")]
    public async Task<HttpResponseData> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var loginData = await FunctionHelper.DeserializeRequestBody<LoginData>(req);
        var ip = req.ExtractIpAddress();
        var authResult = await _mediator.Send(new AuthenticateRequest(loginData, ip));

        var response = req.CreateResponse();
        if (authResult.IsSuccess)
        {
            await response.WriteAsJsonAsync(authResult);
        }
        else
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            await response.WriteAsJsonAsync(authResult);
        }
        
        return response;
    }
    
    [Function("RefreshTokens")]
    public async Task<HttpResponseData> RefreshTokens([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/refreshTokens")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var refreshTokensData = await FunctionHelper.DeserializeRequestBody<RefreshTokensData>(req);
        var ip = req.ExtractIpAddress();
        
        var authResult = await _mediator.Send(new RefreshTokensRequest(refreshTokensData, ip));
        
        var response = req.CreateResponse();
        if (authResult.IsSuccess)
        {
            await response.WriteAsJsonAsync(authResult);
        }
        else
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            await response.WriteAsJsonAsync(authResult);
        }
        
        return response;
    }
}
