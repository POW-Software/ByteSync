using ByteSync.Common.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Authentication;

public class RefreshTokensCommand  : IRequest<RefreshTokensResponse>
{
    
    public RefreshTokensCommand(RefreshTokensData refreshTokensData, String ipAddress)
    {
        RefreshTokensData = refreshTokensData;
        this.ipAddress = ipAddress;
    }

    public RefreshTokensData RefreshTokensData { get; set; }

    public String ipAddress { get; set; }
    
}