using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Serials;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;

namespace ByteSync.ServerCommon.Services.Clients;

public class AuthService : IAuthService
{
    private readonly ITokensFactory _tokensFactory;
    private readonly IByteSyncEndpointFactory _byteSyncEndpointFactory;
    private readonly IClientsRepository _clientsRepository;
    private readonly IClientSoftwareVersionService _clientSoftwareVersionService;

    public AuthService(ITokensFactory tokensFactory, IByteSyncEndpointFactory byteSyncEndpointFactory, IClientsRepository clientsRepository,
        IClientSoftwareVersionService clientSoftwareVersionService)
    {
        _tokensFactory = tokensFactory;
        _byteSyncEndpointFactory = byteSyncEndpointFactory;
        _clientsRepository = clientsRepository;
        _clientSoftwareVersionService = clientSoftwareVersionService;
    }
        
    /// <summary>
    /// 
    /// </summary>
    /// <param name="loginData"></param>
    /// <param name="ipAddress"></param>
    /// <returns></returns>
    public async Task<InitialAuthenticationResponse> Authenticate(LoginData loginData, string ipAddress)
    {
        InitialAuthenticationResponse authenticationResponse;
        
        bool isClientVersionAllowed = await _clientSoftwareVersionService.IsClientVersionAllowed(loginData);
        if (!isClientVersionAllowed)
        {
            authenticationResponse = new InitialAuthenticationResponse(InitialConnectionStatus.VersionNotAllowed);
            return authenticationResponse;
        }
        
        if (loginData.ClientId.IsNullOrEmpty())
        {
            authenticationResponse = new InitialAuthenticationResponse(InitialConnectionStatus.UndefinedClientId);
            return authenticationResponse;
        }
        
        if (loginData.ClientInstanceId.IsNullOrEmpty())
        {
            authenticationResponse = new InitialAuthenticationResponse(InitialConnectionStatus.UndefinedClientInstanceId);
            return authenticationResponse;
        }
            
        if (loginData.OsPlatform == null || loginData.OsPlatform == OSPlatforms.Undefined)
        {
            authenticationResponse = new InitialAuthenticationResponse(InitialConnectionStatus.UnknownOsPlatform);
            return authenticationResponse;
        }

        JwtTokens? tokens = null;
        var result = await _clientsRepository.AddOrUpdate(loginData.ClientInstanceId, client =>
        {
            if (client == null)
            {
                client = new Client(loginData.ClientId, loginData.ClientInstanceId,
                    loginData.Version, loginData.OsPlatform!.Value, ipAddress);
            }
            else
            {
                client.IpAddress = ipAddress;
            }
            
            tokens = _tokensFactory.BuildTokens(client);
            client.RefreshToken = tokens.RefreshToken;

            return client;
        });

        BindSerialResponse bindSerialResponse = await BindSerial(result.Element!, loginData);

        var endPoint = _byteSyncEndpointFactory.BuildByteSyncEndpoint(result.Element!, bindSerialResponse.ProductSerialDescription);
        
        authenticationResponse = new InitialAuthenticationResponse(InitialConnectionStatus.Success, endPoint,
            tokens!.BuildAuthenticationTokens(), bindSerialResponse);

        return authenticationResponse;
    }

    private Task<BindSerialResponse> BindSerial(Client _, LoginData loginData)
    {
        BindSerialResponseStatus bindSerialResponseStatus;
        ProductSerial? productSerial;
        // if (loginData.Email.IsNotEmpty(true) && loginData.Serial.IsNotEmpty(true))
        // {
        //     productSerial = null;
        //     if (productSerial == null)
        //     {
        //         bindSerialResponseStatus = BindSerialResponseStatus.NotFound;
        //     }
        //     else
        //     {
        //         bindSerialResponseStatus = BindSerialResponseStatus.OK;
        //     }
        // }
        // else
        // {
        //     bindSerialResponseStatus = BindSerialResponseStatus.NotSupplied;
        //     productSerial = null;
        // }
        
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

    public async Task<RefreshTokensResponse> RefreshTokens(RefreshTokensData refreshTokensData, string ipAddress)
    {
        RefreshTokensResponse? authenticationResponse = null;
        if (refreshTokensData.Token.IsNullOrEmpty())
        {
            authenticationResponse = new RefreshTokensResponse(RefreshTokensStatus.RefreshTokenNotFound);
            return authenticationResponse;
        }

        JwtTokens? tokens = null;
        var result = await _clientsRepository.AddOrUpdate(refreshTokensData.ClientInstanceId,
            client =>
            {
                if (client == null || client.RefreshToken == null || !Equals(client.RefreshToken.Token, refreshTokensData.Token))
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