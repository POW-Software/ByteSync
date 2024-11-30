using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Factories;

public interface ITokensFactory
{
    public JwtTokens BuildTokens(Client client);
}