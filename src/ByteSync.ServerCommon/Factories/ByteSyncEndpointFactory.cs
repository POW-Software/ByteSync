using System.Security.Claims;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Serials;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Interfaces.Factories;

namespace ByteSync.ServerCommon.Factories;

public class ByteSyncEndpointFactory : IByteSyncEndpointFactory
{
    public ByteSyncEndpoint BuildByteSyncEndpoint(Client client, ProductSerialDescription? productSerialDescription)
    {
        ByteSyncEndpoint byteSyncEndpoint = new ByteSyncEndpoint();
        
        byteSyncEndpoint.ClientId = client.ClientId;
        byteSyncEndpoint.ClientInstanceId = client.ClientInstanceId;
        byteSyncEndpoint.Version = client.Version;
        byteSyncEndpoint.IpAddress = client.IpAddress;
        byteSyncEndpoint.OSPlatform = client.OsPlatform;

        return byteSyncEndpoint;
    }

    public ByteSyncEndpoint? BuildByteSyncEndpoint(ICollection<Claim>? claims)
    {
        if (claims == null)
        {
            return null;
        }
        claims = claims.ToList();
            
        ByteSyncEndpoint byteSyncEndpoint = new ByteSyncEndpoint();
        
        var ipAddress = claims.FirstOrDefault(c => c.Type.Equals(AuthConstants.CLAIM_IP_ADDRESS))?.Value;
        if (ipAddress == null)
        {
            return null;
        }
        byteSyncEndpoint.IpAddress = ipAddress;
                
        var clientInstanceId = claims.FirstOrDefault(c => c.Type.Equals(AuthConstants.CLAIM_CLIENT_INSTANCE_ID))?.Value;
        if (clientInstanceId == null)
        {
            return null;
        }
        byteSyncEndpoint.ClientInstanceId = clientInstanceId;
        
        var clientId = claims.FirstOrDefault(c => c.Type.Equals(AuthConstants.CLAIM_CLIENT_ID))?.Value;
        if (clientId == null)
        {
            return null;
        }
        byteSyncEndpoint.ClientId = clientId;

        var version = claims.FirstOrDefault(c => c.Type.Equals(AuthConstants.CLAIM_VERSION))?.Value;
        if (version == null)
        {
            return null;
        }
        byteSyncEndpoint.Version = version;
        
        var osPlatform = claims.FirstOrDefault(c => c.Type.Equals(AuthConstants.CLAIM_OS_PLATFORM))?.Value;
        if (osPlatform.IsNullOrEmpty())
        {
            return null;
        }
        byteSyncEndpoint.OSPlatform = Enum.Parse<OSPlatforms>(osPlatform!);

        return byteSyncEndpoint;
    }
}