using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Serials;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Authentication;

public class AuthenticateCommandHandler : IRequestHandler<AuthenticateRequest, InitialAuthenticationResponse>
{
    private readonly ITokensFactory _tokensFactory;
    private readonly IByteSyncEndpointFactory _byteSyncEndpointFactory;
    private readonly IClientsRepository _clientsRepository;
    private readonly IClientSoftwareVersionService _clientSoftwareVersionService;
    private readonly IAuthenticationChallengeService _challenges;
    public AuthenticateCommandHandler(ITokensFactory tokensFactory, IByteSyncEndpointFactory byteSyncEndpointFactory, IClientsRepository clientsRepository,
        IClientSoftwareVersionService clientSoftwareVersionService, IAuthenticationChallengeService challenges)
    {
        _tokensFactory = tokensFactory;
        _byteSyncEndpointFactory = byteSyncEndpointFactory;
        _clientsRepository = clientsRepository;
        _clientSoftwareVersionService = clientSoftwareVersionService;
        _challenges = challenges;
    }
    
    public async Task<InitialAuthenticationResponse> Handle(AuthenticateRequest request, CancellationToken cancellationToken)
    {
        InitialAuthenticationResponse authenticationResponse;
        
        bool isClientVersionAllowed = await _clientSoftwareVersionService.IsClientVersionAllowed(request.LoginData);
        if (!isClientVersionAllowed)
        {
            authenticationResponse = new InitialAuthenticationResponse(InitialConnectionStatus.VersionNotAllowed);
            return authenticationResponse;
        }
        
        if (request.LoginData.ClientId.IsNullOrEmpty())
        {
            authenticationResponse = new InitialAuthenticationResponse(InitialConnectionStatus.UndefinedClientId);
            return authenticationResponse;
        }
        
        if (request.LoginData.ClientInstanceId.IsNullOrEmpty())
        {
            authenticationResponse = new InitialAuthenticationResponse(InitialConnectionStatus.UndefinedClientInstanceId);
            return authenticationResponse;
        }
            
        if (request.LoginData.OsPlatform == null || request.LoginData.OsPlatform == OSPlatforms.Undefined)
        {
            authenticationResponse = new InitialAuthenticationResponse(InitialConnectionStatus.UnknownOsPlatform);
            return authenticationResponse;
        }

        if (!await _challenges.VerifyAndConsume(request.LoginData))
            return new InitialAuthenticationResponse(InitialConnectionStatus.InvalidProof);

        JwtTokens? tokens = null;
        var result = await _clientsRepository.AddOrUpdate(request.LoginData.ClientInstanceId, client =>
        {
            if (client == null)
            {
                client = new Client(request.LoginData.Identity, request.LoginData.ClientInstanceId,
                    request.LoginData.Version, request.LoginData.OsPlatform!.Value, request.IpAddress);
                client.Identity = request.LoginData.Identity;
                client.AuthenticationEpoch = AuthenticationProtocol.Epoch;
            }
            else
            {
                if (client.Identity != request.LoginData.Identity || client.AuthenticationEpoch != AuthenticationProtocol.Epoch)
                    return null;
                client.IpAddress = request.IpAddress;
                client.Version = request.LoginData.Version;
                client.OsPlatform = request.LoginData.OsPlatform.Value;
            }
            
            tokens = _tokensFactory.BuildTokens(client);
            client.RefreshToken = tokens.RefreshToken;

            return client;
        });

        if (!result.IsSaved || tokens == null)
            return new InitialAuthenticationResponse(InitialConnectionStatus.InvalidProof);

        BindSerialResponse bindSerialResponse = await BindSerial(result.Element!);

        var endPoint = _byteSyncEndpointFactory.BuildByteSyncEndpoint(result.Element!, bindSerialResponse.ProductSerialDescription);
        
        authenticationResponse = new InitialAuthenticationResponse(InitialConnectionStatus.Success, endPoint,
            tokens!.BuildAuthenticationTokens(), bindSerialResponse);

        return authenticationResponse;
    }
    
    private Task<BindSerialResponse> BindSerial(Client _)
    {
        BindSerialResponseStatus bindSerialResponseStatus;
        
        bindSerialResponseStatus = BindSerialResponseStatus.NotSupplied;

        ProductSerialDescription? productSerialDescription = null;

        BindSerialResponse response = new BindSerialResponse(bindSerialResponseStatus, productSerialDescription);

        return Task.FromResult(response);
    }
    
    public ProductSerialDescription BuildProductSerialDescription(ProductSerial productSerial, SerialStatus serialStatus)
    {
        ProductSerialDescription productSerialDescription = new ProductSerialDescription();

        productSerialDescription.Email = productSerial.Email;
        productSerialDescription.SerialNumber = productSerial.SerialNumber;
        productSerialDescription.ProductName = productSerial.ProductName;
        productSerialDescription.Subscription = productSerial.Subscription;
        productSerialDescription.AllowedCloudSynchronizationVolumeInBytes = 
            productSerial.AllowedCloudSynchronizationVolumeInBytes;

        productSerialDescription.Status = serialStatus;

        return productSerialDescription;
    }    
}
