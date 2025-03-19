using ByteSync.Business.Communications;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Exceptions;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Services.Communications;
using ByteSync.TestsCommon.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Controls.Communications;

public class ConnectionServiceTests
{
    private Mock<IConnectionFactory> _mockConnectionFactory;
    private Mock<IAuthenticationTokensRepository> _mockAuthenticationTokensRepository;
    private Mock<ILogger<ConnectionService>> _mockLogger;
    
    private ConnectionService _connectionService;
    
    [SetUp]
    public void SetUp()
    {
        _mockConnectionFactory = new Mock<IConnectionFactory>(MockBehavior.Strict);
        _mockAuthenticationTokensRepository = new Mock<IAuthenticationTokensRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<ConnectionService>>(MockBehavior.Loose);

        _connectionService = new ConnectionService(
            _mockConnectionFactory.Object,
            _mockAuthenticationTokensRepository.Object,
            _mockLogger.Object
        );
    }
    
    [Test]
    public async Task StartConnectionAsync_ShouldRetryOnException_ExceptVersionNotAllowed()
    {
        // Arrange
        int attemptCount = 0;

        // Configure the mock to fail several times, then succeed
        _mockConnectionFactory
            .Setup(m => m.BuildConnection())
            .Returns(() =>
            {
                attemptCount++;

                // Échoue les 3 premières tentatives avec une exception générique
                if (attemptCount < 3)
                {
                    throw new Exception("Test failure");
                }

                // Fail the first 3 attempts with a generic exception
                return Task.FromResult(new BuildConnectionResult
                {
                    HubConnection = new TestableHubConnection(),
                    EndPoint = new ByteSyncEndpoint { ClientId = "test", ClientInstanceId = "test" },
                    AuthenticateResponseStatus = InitialConnectionStatus.Success
                });
            });

        // Keeps a record of the connection status
        var statusChanges = new List<ConnectionStatuses>();
        using var statusSubscription = _connectionService.ConnectionStatus.Subscribe(status => statusChanges.Add(status));

        // Act
        await _connectionService.StartConnectionAsync();

        // Assert
        attemptCount.Should().Be(3, "The system should have made 3 attempts");

        // Check the sequence of connection statuses
        statusChanges.Should().ContainInOrder(
            ConnectionStatuses.Connecting, // First attempt
            ConnectionStatuses.NotConnected, // First failure
            ConnectionStatuses.Connecting, // Second attempt
            ConnectionStatuses.NotConnected, // Second failure
            ConnectionStatuses.Connecting, // Third attempt
            ConnectionStatuses.Connected // h failure
        );
    }

    [Test]
    [CancelAfter(2000)]
    public async Task StartConnectionAsync_ShouldNotRetryOnVersionNotAllowedException()
    {
        // Arrange
        int attemptCount = 0;

        // Configure the mock to throw an exception VersionNotAllowed
        _mockConnectionFactory
            .Setup(m => m.BuildConnection())
            .Returns(() =>
            {
                attemptCount++;

                return Task.FromResult(new BuildConnectionResult
                {
                    HubConnection = null,
                    EndPoint = null,
                    AuthenticateResponseStatus = InitialConnectionStatus.VersionNotAllowed
                });
            });

        // Act & Assert
        await FluentActions.Invoking(async () => await _connectionService.StartConnectionAsync())
            .Should()
            .ThrowAsync<BuildConnectionException>()
            .WithMessage("*Unable to connect*")
            .Where(ex => ex.InitialConnectionStatus == InitialConnectionStatus.VersionNotAllowed);

        attemptCount.Should().Be(1, "The system should not retry with an exception VersionNotAllowed");
    }
}