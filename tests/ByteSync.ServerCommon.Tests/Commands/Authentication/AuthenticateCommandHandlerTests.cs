using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Serials;
using ByteSync.ServerCommon.Commands.Authentication;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using FluentAssertions;

namespace ByteSync.ServerCommon.Tests.Commands.Authentication;

public class AuthenticateCommandHandlerTests
{
    private IAuthService _mockAuthService;
    private AuthenticateCommandHandler _authenticateCommandHandler;

    [SetUp]
    public void Setup()
    {
        _mockAuthService = A.Fake<IAuthService>();
        _authenticateCommandHandler = new AuthenticateCommandHandler(_mockAuthService);
    }

    [Test]
    public async Task Handle_ValidRequest_CallsAuthServiceAndReturnsResponse()
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
        var request = new AuthenticateCommand(loginData, ipAddress);

        var expectedEndpoint = new ByteSyncEndpoint();
        var expectedTokens = new AuthenticationTokens();
        var expectedBindSerialResponse = new BindSerialResponse { Status = BindSerialResponseStatus.Ignored };

        var expectedResponse = new InitialAuthenticationResponse(
            InitialConnectionStatus.Success,
            expectedEndpoint,
            expectedTokens,
            expectedBindSerialResponse
        );

        A.CallTo(() => _mockAuthService.Authenticate(loginData, ipAddress))
            .Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _authenticateCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResponse);

        A.CallTo(() => _mockAuthService.Authenticate(
            A<LoginData>.That.Matches(ld =>
                ld.ClientId == loginData.ClientId &&
                ld.ClientInstanceId == loginData.ClientInstanceId &&
                ld.Version == loginData.Version &&
                ld.OsPlatform == loginData.OsPlatform),
            ipAddress)).MustHaveHappenedOnceExactly();

    }
}