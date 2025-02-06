using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using ByteSync.Functions.Http;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ByteSync.Functions.Helpers.Middlewares;

public class JwtMiddleware : IFunctionsWorkerMiddleware
{
    private readonly string _secret;
    private readonly IClientsRepository _clientsRepository;
    private readonly ILogger<JwtMiddleware> _logger;

    private readonly HashSet<string> _allowedAnonymousFunctionEntryPoints;
    
    public JwtMiddleware(IOptions<AppSettings> appSettings, IClientsRepository clientsRepository, ILogger<JwtMiddleware> logger)
    {
        var loginFunctionEntryPoint = GetEntryPoint<AuthFunction>(nameof(AuthFunction.Login));
        _allowedAnonymousFunctionEntryPoints = new HashSet<string>
        {
            loginFunctionEntryPoint
        };
        
        _secret = appSettings.Value.Secret;
        _clientsRepository = clientsRepository;
        _logger = logger;
    }
    
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var functionEntryPoint = context.FunctionDefinition.EntryPoint;
        if (_allowedAnonymousFunctionEntryPoints.Contains(functionEntryPoint))
        {
            await next(context);
            return;
        }
        
        var requestData = await context.GetHttpRequestDataAsync();

        var authorizationHeader = requestData?.Headers.FirstOrDefault(p => p.Key.Equals("Authorization"));

        var token = authorizationHeader?.Value?.LastOrDefault();

        if (token != null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secret);

            try
            {
                var claims = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidIssuer = AuthConstants.ISSUER,
                    ValidateAudience = true,
                    ValidAudience = AuthConstants.AUDIENCE,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);
                
                if (validatedToken.ValidTo < DateTime.UtcNow)
                {
                    throw new SecurityTokenExpiredException("Token is expired");
                }

                if (claims != null)
                {
                    var clientInstanceId = claims.Claims.FirstOrDefault(c => c.Type.Equals(AuthConstants.CLAIM_CLIENT_INSTANCE_ID))?.Value;
                    if (clientInstanceId == null)
                    {
                        throw new SecurityTokenExpiredException("clientInstanceId is null");
                    }
                    
                    var client = await _clientsRepository.Get(clientInstanceId);
                    if (client == null)
                    {
                        throw new SecurityTokenExpiredException("Client is null");
                    }
                    
                    context.Items.Add(AuthConstants.FUNCTION_CONTEXT_CLIENT, client);
                }
                
                await next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                
                await HandleTokenError(context, "Invalid token");
            }
        }
        else
        {
            await HandleTokenError(context, "Token not provided");
        }
    }

    private static async Task HandleTokenError(FunctionContext context, string message)
    {
        var httpReqData = await context.GetHttpRequestDataAsync();
        if (httpReqData != null)
        {
            var newHttpResponse = httpReqData.CreateResponse(HttpStatusCode.Unauthorized);
            await newHttpResponse.WriteAsJsonAsync(new { ResponseStatus = message }, newHttpResponse.StatusCode);
            context.GetInvocationResult().Value = newHttpResponse;
        }
    }
    
    private string GetEntryPoint<T>(string methodName)
    {
        return typeof(T).Namespace + "." + typeof(T).Name + "." + methodName;
    }
}