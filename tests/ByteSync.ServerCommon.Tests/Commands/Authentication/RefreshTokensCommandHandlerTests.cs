using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Misc;
using ByteSync.ServerCommon.Commands.Authentication;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using FluentAssertions;

namespace ByteSync.ServerCommon.Tests.Commands.Authentication;

public class RefreshTokensCommandHandlerTests
{
    private IAuthService _mockAuthService;
    private RefreshTokensCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockAuthService = A.Fake<IAuthService>();
        _handler = new RefreshTokensCommandHandler(_mockAuthService);
    }

    [Test]
    public async Task Handle_ValidRequest_ReturnsSuccessfulRefreshTokensResponse()
    {
        // Arrange
        var refreshTokensData = new RefreshTokensData
        {
            Token = "some-refresh-token",
            ClientInstanceId = "client-123",
            Version = "1.0.0",
            OsPlatform = OSPlatforms.Windows 
        };

        var ipAddress = "127.0.0.1";

        var request = new RefreshTokensCommand(refreshTokensData, ipAddress);
            
        var expectedTokens = new AuthenticationTokens
        {
            JwtToken = "access-token",
            JwtTokenDurationInSeconds = 3600,
            RefreshToken = "refresh-token",
            RefreshTokenExpiration = DateTimeOffset.UtcNow.AddDays(7)
        };

        var expectedResponse = new RefreshTokensResponse(
            RefreshTokensStatus.RefreshTokenOk,
            expectedTokens);

        A.CallTo(() => _mockAuthService.RefreshTokens(refreshTokensData, ipAddress))
            .Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.RefreshTokensStatus.Should().Be(RefreshTokensStatus.RefreshTokenOk);
        result.AuthenticationTokens.Should().BeEquivalentTo(expectedTokens);

        A.CallTo(() => _mockAuthService.RefreshTokens(refreshTokensData, ipAddress))
            .MustHaveHappenedOnceExactly();
    }
}