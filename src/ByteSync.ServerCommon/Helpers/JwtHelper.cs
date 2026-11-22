using System.Text;
using ByteSync.ServerCommon.Business.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ByteSync.ServerCommon.Helpers;

public static class JwtHelper
{
    public static void AddJwtAuthentication(this IServiceCollection services, string secret)
    {
        var key = Encoding.ASCII.GetBytes(secret);
        
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(option =>
            {
                option.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var instance = context.Principal?.FindFirst(AuthConstants.CLAIM_CLIENT_INSTANCE_ID)?.Value;
                        var repository = context.HttpContext.RequestServices.GetRequiredService<ByteSync.ServerCommon.Interfaces.Repositories.IClientsRepository>();
                        var client = instance == null ? null : await repository.Get(instance);
                        if (client == null) { context.Fail("Unknown client"); return; }
                        try { ByteSync.ServerCommon.Security.ClientAuthentication.ValidateClaims(client, context.Principal!); }
                        catch (SecurityTokenValidationException) { context.Fail("Authentication epoch or identity mismatch"); }
                    }
                };
                option.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,

                    ClockSkew = TimeSpan.Zero,

                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidIssuer = AuthConstants.ISSUER,
                    ValidAudience = AuthConstants.AUDIENCE,
                    IssuerSigningKey = new SymmetricSecurityKey(key), 
                };
            });
    }
    
    public static void AddClaimAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(option =>
        {
            option.AddPolicy(AuthConstants.CLAIMBASEDAUTH, policy =>
            {
                policy.RequireClaim(AuthConstants.CLAIM_AUTHENTICATION_EPOCH, ByteSync.Common.Business.Auth.AuthenticationProtocol.Epoch.ToString());
                policy.RequireClaim(AuthConstants.CLAIM_IDENTITY);
                policy.RequireClaim(AuthConstants.CLAIM_CLIENT_ID);
                policy.RequireClaim(AuthConstants.CLAIM_CLIENT_INSTANCE_ID);
                policy.RequireClaim(AuthConstants.CLAIM_VERSION);
            });

            option.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim(AuthConstants.CLAIM_AUTHENTICATION_EPOCH, ByteSync.Common.Business.Auth.AuthenticationProtocol.Epoch.ToString())
                .RequireClaim(AuthConstants.CLAIM_IDENTITY)
                .Build();
        });
    }
}