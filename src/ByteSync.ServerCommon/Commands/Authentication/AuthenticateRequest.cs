using ByteSync.Common.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Authentication;

public class AuthenticateRequest : IRequest<InitialAuthenticationResponse>
{
    public AuthenticateRequest(LoginData loginData, String ipAddress)
    {
        LoginData = loginData;
        IpAddress = ipAddress;
    }

    public LoginData LoginData { get; set; }

    public String IpAddress { get; set; }
}