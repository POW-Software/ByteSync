using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Misc;
using ByteSync.ServerCommon.Commands.Authentication;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using FakeItEasy;
using FluentAssertions;

namespace ByteSync.ServerCommon.Tests.Commands.Authentication;

public class RefreshTokensCommandHandlerTests
{
    private RefreshTokensCommandHandler _handler;
    private ITokensFactory _mockTokensFactory;
    private IClientsRepository _mockClientsRepository;
    
    [SetUp]
    public void Setup()
    {
        _mockTokensFactory = A.Fake<ITokensFactory>();
        _mockClientsRepository = A.Fake<IClientsRepository>();
        _handler = new RefreshTokensCommandHandler(_mockTokensFactory, _mockClientsRepository);
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

        var request = new RefreshTokensRequest(refreshTokensData, ipAddress);

        var expectedRefreshToken = new ByteSync.ServerCommon.Business.Auth.RefreshToken
        {
            Token = "refresh-token",
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Created = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress
        };

        var expectedTokens = new AuthenticationTokens
        {
            JwtToken = "access-token",
            JwtTokenDurationInSeconds = 3600,
            RefreshToken = expectedRefreshToken.Token,
            RefreshTokenExpiration = expectedRefreshToken.Expires
        };

        var expectedJwtTokens = new ByteSync.ServerCommon.Business.Auth.JwtTokens(
            expectedTokens.JwtToken,
            expectedRefreshToken,
            expectedTokens.JwtTokenDurationInSeconds
        );

        var client = new ByteSync.ServerCommon.Business.Auth.Client
        {
            ClientInstanceId = refreshTokensData.ClientInstanceId,
            RefreshToken = expectedRefreshToken
        };

        // Mock the tokens factory
        A.CallTo(() => _mockTokensFactory.BuildTokens(A<ByteSync.ServerCommon.Business.Auth.Client>.Ignored))
            .Returns(expectedJwtTokens);

        // Mock the repository AddOrUpdate
        A.CallTo(() => _mockClientsRepository.AddOrUpdate(
                A<string>.Ignored,
                A<Func<ByteSync.ServerCommon.Business.Auth.Client?, ByteSync.ServerCommon.Business.Auth.Client?>>.Ignored))
            .ReturnsLazily((string id, Func<ByteSync.ServerCommon.Business.Auth.Client?, ByteSync.ServerCommon.Business.Auth.Client?> func) =>
            {
                var updatedClient = func(client);
                return ByteSync.ServerCommon.Tests.Helpers.UpdateResultBuilder.BuildAddOrUpdateResult(updatedClient, false);
            });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.RefreshTokensStatus.Should().Be(RefreshTokensStatus.RefreshTokenOk);
        result.AuthenticationTokens.Should().BeEquivalentTo(expectedTokens);
    }
}