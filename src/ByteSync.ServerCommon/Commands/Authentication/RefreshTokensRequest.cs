using ByteSync.Common.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Authentication;

public class RefreshTokensRequest  : IRequest<RefreshTokensResponse>
{
    
    public RefreshTokensRequest(RefreshTokensData refreshTokensData, String ipAddress)
    {
        RefreshTokensData = refreshTokensData;
        IpAddress = ipAddress;
    }

    public RefreshTokensData RefreshTokensData { get; set; }

    public String IpAddress { get; set; }
    
}