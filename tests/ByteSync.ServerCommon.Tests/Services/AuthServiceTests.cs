using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Serials;
using ByteSync.Common.Business.Versions;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Misc;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Services;
using FakeItEasy;

namespace ByteSync.ServerCommon.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private ITokensFactory _mockTokensBuilder;
    private IByteSyncEndpointFactory _mockByteSyncEndpointFactory;
    private IClientsRepository _mockClientRepository;

    private AuthService _authService;
    private List<Client> _clients;
    private IClientSoftwareVersionService _mockClientSoftwareVersionService;

    [SetUp]
    public void Setup()
    {
        _mockTokensBuilder = A.Fake<ITokensFactory>();
        _mockByteSyncEndpointFactory = A.Fake<IByteSyncEndpointFactory>();
        _mockClientRepository = A.Fake<IClientsRepository>();
        _mockClientSoftwareVersionService = A.Fake<IClientSoftwareVersionService>();
        
        _clients = new List<Client>();

        _authService = new AuthService(_mockTokensBuilder, _mockByteSyncEndpointFactory, _mockClientRepository, 
            _mockClientSoftwareVersionService); 
    }
    
    [Test]
    public async Task Authenticate_WithValidLoginData_NoSerial_ReturnsSuccessfulAuthenticationResponse()
    {
        // Arrange
        var loginData = new LoginData
        {
            Version = "1.0.1",
            ClientId = "ClientId",
            ClientInstanceId = "ClientInstanceId",
            OsPlatform = OSPlatforms.Windows
        }; 
        string ipAddress = "127.0.0.1";
        
        InitializeDefaultMocks();

        // Act
        var result = await _authService.Authenticate(loginData, ipAddress);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.InitialConnectionStatus, Is.EqualTo(InitialConnectionStatus.Success));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.BindSerialResponse.Status, Is.EqualTo(BindSerialResponseStatus.NotSupplied));
            Assert.That(result.BindSerialResponse.ProductSerialDescription, Is.Null);
        });                     
    }
    
    [Test]
    public async Task Authenticate_WithValidLoginData_UnknownSerial_ReturnsSuccessfulAuthenticationResponse()
    {
        // Arrange
        var loginData = new LoginData
        {
            Version = "1.0.1",
            ClientId = "ClientId",
            ClientInstanceId = "ClientInstanceId",
            OsPlatform = OSPlatforms.Windows
        }; 
        string ipAddress = "127.0.0.1";
        
        InitializeDefaultMocks();

        // Act
        var result = await _authService.Authenticate(loginData, ipAddress);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.InitialConnectionStatus, Is.EqualTo(InitialConnectionStatus.Success));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.BindSerialResponse.Status, Is.EqualTo(BindSerialResponseStatus.NotSupplied));
            Assert.That(result.BindSerialResponse.ProductSerialDescription, Is.Null);
        });
        
        A.CallTo(() => _mockClientSoftwareVersionService.IsClientVersionAllowed(A<LoginData>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockTokensBuilder.BuildTokens(A<Client>.Ignored)).MustHaveHappenedOnceExactly();
        // _mockClientSoftwareVersionService.Verify();
        // _mockTokensBuilder.Verify();
    }
    
    // [Test]
    // public async Task Authenticate_WithValidLoginData_6ClientsWithKnownSerial_SerialStatus_NoAvailableSlot()
    // {
    //     // Arrange
    //     InitializeDefaultMocks();
    //
    //     LoginData loginData;
    //     string ipAddress = "127.0.0.1";
    //     
    //     for (int i = 0; i < 5; i++)
    //     {
    //         loginData = new LoginData
    //         {
    //             Version = "1.0.1",
    //             Email = "email@eml.com",
    //             Machinename = "MyMachine_" + i,
    //             ClientId = "ClientId_" + i,
    //             ClientInstanceId = "ClientInstanceId_" + i,
    //             Serial = "KnownSerial",
    //             OsPlatform = OSPlatforms.Windows
    //         }; 
    //         
    //         await _authService.Authenticate(loginData, ipAddress);
    //     }
    //     
    //     loginData = new LoginData
    //     {
    //         Version = "1.0.1",
    //         Email = "email@eml.com",
    //         Machinename = "MyMachine_Bad",
    //         ClientId = "ClientId_Bad",
    //         ClientInstanceId = "ClientInstanceId_Bad",
    //         Serial = "KnownSerial",
    //         OsPlatform = OSPlatforms.Windows
    //     }; 
    //         
    //     var result = await _authService.Authenticate(loginData, ipAddress);
    //         
    //     // Assert
    //     Assert.Multiple(() =>
    //     {
    //         Assert.That(result, Is.Not.Null);
    //         Assert.That(result.InitialConnectionStatus, Is.EqualTo(InitialConnectionStatus.Success));
    //         Assert.That(result.IsSuccess, Is.True);
    //         Assert.That(result.BindSerialResponse.Status, Is.EqualTo(BindSerialResponseStatus.NotFound));
    //         Assert.That(result.BindSerialResponse.ProductSerialDescription, Is.Null);
    //     });
    //
    //     // _mockClientSoftwareVersionService.Verify();
    //     // _mockTokensBuilder.Verify();
    //     // _mockSerialsHolder.Verify();
    // }

    private void InitializeDefaultMocks()
    {
        A.CallTo(() => _mockClientSoftwareVersionService.GetClientSoftwareVersionSettings())
            .Returns(new ClientSoftwareVersionSettings
            {
                MandatoryVersion = new SoftwareVersion { Version = "1.0.0" }
            });
        
        A.CallTo(() => _mockClientSoftwareVersionService.IsClientVersionAllowed(A<LoginData>.Ignored))
            .Returns(true);

        var client = new Client();
        A.CallTo(() => _mockClientRepository.AddOrUpdate(A<string>.Ignored, A<Func<Client?, Client?>>.Ignored))
            .Invokes((string _, Func<Client?, Client?> func) => func(client))
            .Returns(new UpdateEntityResult<Client>(client, UpdateEntityStatus.Saved));

        A.CallTo(() => _mockTokensBuilder.BuildTokens(A<Client>.Ignored))
            .Returns(new JwtTokens("jwtToken", new RefreshToken(), 3));
    }
    
    // [Test]
    // public async Task RefreshToken_WithValidRefreshTokenData_ReturnsSuccessfulAuthenticationResponse()
    // {
    //     // Arrange
    //     var refreshTokenData = new RefreshTokenData { ... }; // Remplissez avec des données valides
    //     string ipAddress = "127.0.0.1";
    //
    //     // Ici, vous définissez le comportement attendu de vos dépendances mockées.
    //     // Par exemple:
    //     _mockClientRepository.Setup(x => x.Find(It.IsAny<Func<Client, bool>>())).Returns(new List<Client> { new Client { ... } }); // Remplissez avec des données valides
    //
    //     // Ajoutez d'autres comportements mockés si nécessaire...
    //
    //     // Act
    //     var result = await _authService.RefreshToken(refreshTokenData, ipAddress);
    //
    //     // Assert
    //     Assert.That(result, Is.Not.Null);
    //     Assert.That(result.RefreshTokenStatus, Is.EqualTo(RefreshTokenStatus.RefreshTokenOk));
    // }
}