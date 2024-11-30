using System.Security.Claims;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Serials;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Factories;

public interface IByteSyncEndpointFactory
{
    public ByteSyncEndpoint BuildByteSyncEndpoint(Client client, ProductSerialDescription? productSerialDescription);

    public ByteSyncEndpoint? BuildByteSyncEndpoint(ICollection<Claim>? claims);
}