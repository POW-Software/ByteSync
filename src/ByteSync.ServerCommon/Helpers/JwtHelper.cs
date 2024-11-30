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
                option.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,

                    ClockSkew = TimeSpan.Zero,

                    ValidateIssuerSigningKey = false,
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
                policy.RequireClaim(AuthConstants.CLAIM_CLIENT_ID);
                policy.RequireClaim(AuthConstants.CLAIM_CLIENT_INSTANCE_ID);
                policy.RequireClaim(AuthConstants.CLAIM_VERSION);
            });

            option.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
    }
}