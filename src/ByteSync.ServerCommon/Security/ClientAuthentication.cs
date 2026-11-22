using System.Security.Claims;
using ByteSync.Common.Business.Auth;
using ByteSync.ServerCommon.Business.Auth;
using Microsoft.IdentityModel.Tokens;

namespace ByteSync.ServerCommon.Security;

public static class ClientAuthentication
{
    public static void ValidateClaims(Client client, ClaimsPrincipal claims)
    {
        if (client.AuthenticationEpoch != AuthenticationProtocol.Epoch ||
            claims.FindFirst(AuthConstants.CLAIM_AUTHENTICATION_EPOCH)?.Value != AuthenticationProtocol.Epoch.ToString() ||
            claims.FindFirst(AuthConstants.CLAIM_IDENTITY)?.Value != client.Identity ||
            claims.FindFirst(AuthConstants.CLAIM_CLIENT_INSTANCE_ID)?.Value != client.ClientInstanceId ||
            !AuthenticationProtocol.IsInstanceOf(client.ClientInstanceId, client.Identity ?? ""))
            throw new SecurityTokenValidationException("Authentication epoch or identity mismatch");
    }
}
