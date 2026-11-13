using ByteSync.Common.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface IAuthenticationChallengeService
{
    Task<AuthenticationChallenge> Create(LoginData login);
    Task<bool> VerifyAndConsume(LoginData login);
}
