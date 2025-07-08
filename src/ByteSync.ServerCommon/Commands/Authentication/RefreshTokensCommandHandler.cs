using ByteSync.Common.Business.Auth;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Authentication;

public class RefreshTokensCommandHandler: IRequestHandler<RefreshTokensRequest, RefreshTokensResponse>
{
    private readonly ITokensFactory _tokensFactory;
    private readonly IClientsRepository _clientsRepository;
    public RefreshTokensCommandHandler(ITokensFactory tokensFactory, IClientsRepository clientsRepository)
    {
        _tokensFactory = tokensFactory;
        _clientsRepository = clientsRepository;
    }

    public async Task<RefreshTokensResponse> Handle(RefreshTokensRequest req, CancellationToken cancellationToken)
    { 
        RefreshTokensResponse? authenticationResponse = null;
        if (req.RefreshTokensData.Token.IsNullOrEmpty())
        {
            authenticationResponse = new RefreshTokensResponse(RefreshTokensStatus.RefreshTokenNotFound);
            return authenticationResponse;
        }

        JwtTokens? tokens = null;
        var result = await _clientsRepository.AddOrUpdate(req.RefreshTokensData.ClientInstanceId,
            client =>
            {
                if (client == null || client.RefreshToken == null || !Equals(client.RefreshToken.Token, req.RefreshTokensData.Token))
                {
                    authenticationResponse = new RefreshTokensResponse(RefreshTokensStatus.RefreshTokenNotFound);
                    return null;
                }

                if (client.RefreshToken!.IsExpired)
                {
                    authenticationResponse = new RefreshTokensResponse(RefreshTokensStatus.RefreshTokenNotActive);
                    return null;
                }
            
                tokens = _tokensFactory.BuildTokens(client);
                client.RefreshToken = tokens.RefreshToken;

                return client;
            });

        if (result.IsSaved)
        {
            authenticationResponse =
                new RefreshTokensResponse(RefreshTokensStatus.RefreshTokenOk, tokens!.BuildAuthenticationTokens());

            return authenticationResponse;
        }
        else
        {
            return authenticationResponse!;
        }
    }  
}
