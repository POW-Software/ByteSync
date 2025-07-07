using ByteSync.Common.Business.Auth;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Authentication;

public class AuthenticateCommandHandler : IRequestHandler<AuthenticateCommand, InitialAuthenticationResponse>
{
    private readonly IAuthService _authService;

    public AuthenticateCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }
    
    public async Task<InitialAuthenticationResponse> Handle(AuthenticateCommand request, CancellationToken cancellationToken)
    {
        return await _authService.Authenticate(request.LoginData, request.ipAddress);
    }
}