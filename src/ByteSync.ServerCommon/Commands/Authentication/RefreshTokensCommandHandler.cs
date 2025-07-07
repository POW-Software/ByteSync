using ByteSync.Common.Business.Auth;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Authentication;

public class RefreshTokensCommandHandler: IRequestHandler<RefreshTokensCommand, RefreshTokensResponse>
{
    private readonly IAuthService _authService;

    public RefreshTokensCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<RefreshTokensResponse> Handle(RefreshTokensCommand req, CancellationToken cancellationToken)
    {
        return await _authService.RefreshTokens(req.RefreshTokensData, req.ipAddress);
    }  
}
