using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Serials;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Authentication;

public class AuthenticateCommandHandler : IRequestHandler<AuthenticateRequest, InitialAuthenticationResponse>
{
    private readonly ITokensFactory _tokensFactory;
    private readonly IByteSyncEndpointFactory _byteSyncEndpointFactory;
    private readonly IClientsRepository _clientsRepository;
    private readonly IClientSoftwareVersionService _clientSoftwareVersionService;
    public AuthenticateCommandHandler(ITokensFactory tokensFactory, IByteSyncEndpointFactory byteSyncEndpointFactory, IClientsRepository clientsRepository,
        IClientSoftwareVersionService clientSoftwareVersionService)
    {
        _tokensFactory = tokensFactory;
        _byteSyncEndpointFactory = byteSyncEndpointFactory;
        _clientsRepository = clientsRepository;
        _clientSoftwareVersionService = clientSoftwareVersionService;
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

        JwtTokens? tokens = null;
        var result = await _clientsRepository.AddOrUpdate(request.LoginData.ClientInstanceId, client =>
        {
            if (client == null)
            {
                client = new Client(request.LoginData.ClientId, request.LoginData.ClientInstanceId,
                    request.LoginData.Version, request.LoginData.OsPlatform!.Value, request.IpAddress);
            }
            else
            {
                client.IpAddress = request.IpAddress;
            }
            
            tokens = _tokensFactory.BuildTokens(client);
            client.RefreshToken = tokens.RefreshToken;

            return client;
        });

        BindSerialResponse bindSerialResponse = await BindSerial(result.Element!, request.LoginData);

        var endPoint = _byteSyncEndpointFactory.BuildByteSyncEndpoint(result.Element!, bindSerialResponse.ProductSerialDescription);
        
        authenticationResponse = new InitialAuthenticationResponse(InitialConnectionStatus.Success, endPoint,
            tokens!.BuildAuthenticationTokens(), bindSerialResponse);

        return authenticationResponse;
    }
    
    private Task<BindSerialResponse> BindSerial(Client _, LoginData loginData)
    {
        BindSerialResponseStatus bindSerialResponseStatus;
        ProductSerial? productSerial;
        
        bindSerialResponseStatus = BindSerialResponseStatus.NotSupplied;
        productSerial = null;

        ProductSerialDescription? productSerialDescription = null;
        if (productSerial != null)
        {
            SerialStatus serialStatus;
            if (productSerial.IsExpired)
            {
                serialStatus = SerialStatus.Expired;
            }
            else
            {
                bool bindOK = true;

                if (bindOK)
                {
                    serialStatus = SerialStatus.OK;
                }
                else
                {
                    serialStatus = SerialStatus.NoAvailableSlot;
                }
            }

            productSerialDescription = BuildProductSerialDescription(productSerial, serialStatus);
        }

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