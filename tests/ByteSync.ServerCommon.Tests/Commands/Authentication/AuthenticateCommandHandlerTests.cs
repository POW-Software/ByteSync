using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Serials;
using ByteSync.ServerCommon.Commands.Authentication;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using FluentAssertions;

namespace ByteSync.ServerCommon.Tests.Commands.Authentication;

public class AuthenticateCommandHandlerTests
{
    private ITokensFactory _mockTokensFactory;
    private IByteSyncEndpointFactory _mockEndpointFactory;
    private IClientsRepository _mockClientsRepository;
    private IClientSoftwareVersionService _mockClientSoftwareVersionService;
    private AuthenticateCommandHandler _authenticateCommandHandler;

    [SetUp]
    public void Setup()
    {
        _mockTokensFactory = A.Fake<ITokensFactory>();
        _mockEndpointFactory = A.Fake<IByteSyncEndpointFactory>();
        _mockClientsRepository = A.Fake<IClientsRepository>();
        _mockClientSoftwareVersionService = A.Fake<IClientSoftwareVersionService>();
        _authenticateCommandHandler = new AuthenticateCommandHandler(
            _mockTokensFactory,
            _mockEndpointFactory,
            _mockClientsRepository,
            _mockClientSoftwareVersionService
        );
    }

    [Test]
    public async Task Handle_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var loginData = new LoginData
        {
            ClientId = "client123",
            ClientInstanceId = "instance456",
            Version = "1.0.0",
            OsPlatform = OSPlatforms.Windows
        };

        var ipAddress = "192.168.1.1";
        var request = new AuthenticateRequest(loginData, ipAddress);

        var client = new ByteSync.ServerCommon.Business.Auth.Client(
            loginData.ClientId,
            loginData.ClientInstanceId,
            loginData.Version,
            loginData.OsPlatform!.Value,
            ipAddress
        );

        var expectedRefreshToken = new ByteSync.ServerCommon.Business.Auth.RefreshToken
        {
            Token = "refresh-token",
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Created = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress
        };

        var expectedJwtTokens = new ByteSync.ServerCommon.Business.Auth.JwtTokens(
            "access-token",
            expectedRefreshToken,
            3600
        );

        var expectedTokens = expectedJwtTokens.BuildAuthenticationTokens();
        var expectedBindSerialResponse = new BindSerialResponse { Status = BindSerialResponseStatus.Ignored };
        var expectedEndpoint = new ByteSync.Common.Business.EndPoints.ByteSyncEndpoint();

        // Mock version check
        A.CallTo(() => _mockClientSoftwareVersionService.IsClientVersionAllowed(loginData)).Returns(true);
        // Mock tokens factory
        A.CallTo(() => _mockTokensFactory.BuildTokens(A<ByteSync.ServerCommon.Business.Auth.Client>.Ignored)).Returns(expectedJwtTokens);
        // Mock repository AddOrUpdate
        A.CallTo(() => _mockClientsRepository.AddOrUpdate(
                A<string>.Ignored,
                A<Func<ByteSync.ServerCommon.Business.Auth.Client?, ByteSync.ServerCommon.Business.Auth.Client?>>.Ignored))
            .ReturnsLazily((string id, Func<ByteSync.ServerCommon.Business.Auth.Client?, ByteSync.ServerCommon.Business.Auth.Client?> func) =>
            {
                var updatedClient = func(client);
                return ByteSync.ServerCommon.Tests.Helpers.UpdateResultBuilder.BuildAddOrUpdateResult(updatedClient, false);
            });
        // Mock endpoint factory
        A.CallTo(() => _mockEndpointFactory.BuildByteSyncEndpoint(A<ByteSync.ServerCommon.Business.Auth.Client>.Ignored, A<ProductSerialDescription?>.Ignored))
            .Returns(expectedEndpoint);

        // Act
        var result = await _authenticateCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.InitialConnectionStatus.Should().Be(InitialConnectionStatus.Success);
        result.EndPoint.Should().Be(expectedEndpoint);
        result.AuthenticationTokens.Should().BeEquivalentTo(expectedTokens);
        result.BindSerialResponse.Status.Should().Be(BindSerialResponseStatus.NotSupplied);
    }
}