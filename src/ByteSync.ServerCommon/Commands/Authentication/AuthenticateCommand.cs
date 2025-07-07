using ByteSync.Common.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Authentication;

public class AuthenticateCommand : IRequest<InitialAuthenticationResponse>
{
    public AuthenticateCommand(LoginData loginData, String ipAddress)
    {
        LoginData = loginData;
        this.IpAddress = ipAddress;
    }

    public LoginData LoginData { get; set; }

    public String IpAddress { get; set; }
}