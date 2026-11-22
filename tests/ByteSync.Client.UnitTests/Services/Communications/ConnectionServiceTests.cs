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

namespace ByteSync.Client.UnitTests.Services.Communications;

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
        
        _connectionService.RetryDelaySleepDurationProvider = _ => TimeSpan.Zero;
    }

    [TearDown]
    public void TearDown() => _connectionService.Dispose();

    [TestCase(false)]
    [TestCase(true)]
    public async Task StopConnection_DrainsInflightRefreshWithoutRestartingWithOldCredentials(bool refreshFails)
    {
        var began = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var refresh = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _mockAuthenticationTokensRepository.Setup(r => r.GetTokens()).ReturnsAsync(new AuthenticationTokens { JwtTokenDurationInSeconds = 4 });
        _mockConnectionFactory.Setup(f => f.BuildConnection()).ReturnsAsync(new BuildConnectionResult
            { HubConnection = new TestableHubConnection(), EndPoint = new ByteSyncEndpoint { ClientInstanceId = "old-instance" } });
        _mockConnectionFactory.Setup(f => f.RefreshAuthenticationTokens()).Returns(() => { began.SetResult(); return refresh.Task; });
        await _connectionService.StartConnectionAsync();
        await began.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var stop = _connectionService.StopConnection();
        stop.IsCompleted.Should().BeFalse();
        if (refreshFails) refresh.SetException(new InvalidOperationException("old refresh failed"));
        else refresh.SetResult(false);
        await stop.WaitAsync(TimeSpan.FromSeconds(10));

        _connectionService.ClientInstanceId.Should().Be("old-instance");
        _connectionService.CurrentConnectionStatus.Should().Be(ConnectionStatuses.NotConnected);
        _mockConnectionFactory.Verify(f => f.BuildConnection(), Times.Once());
        _mockConnectionFactory.Verify(f => f.RefreshAuthenticationTokens(), Times.Once());
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task AutomaticRestart_ReleasesTheLifecycleGateAfterSuccessOrAnIncompatibleRelay(bool incompatible)
    {
        var restarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _mockAuthenticationTokensRepository.Setup(r => r.GetTokens()).ReturnsAsync(new AuthenticationTokens { JwtTokenDurationInSeconds = 4 });
        var calls = 0;
        _mockConnectionFactory.Setup(f => f.BuildConnection()).Returns(() =>
        {
            calls++;
            if (calls == 2 && incompatible) throw new RelayIncompatibleException(new ApiException("Missing auth/challenge"));
            return Task.FromResult(new BuildConnectionResult { HubConnection = new TestableHubConnection(),
                EndPoint = new ByteSyncEndpoint { ClientInstanceId = "instance-" + calls } });
        });
        _mockConnectionFactory.Setup(f => f.RefreshAuthenticationTokens()).ReturnsAsync(false);
        using var subscription = _connectionService.ConnectionStatus.Subscribe(status =>
        {
            if (calls == 2 && status == (incompatible ? ConnectionStatuses.RelayIncompatible : ConnectionStatuses.Connected)) restarted.TrySetResult();
        });

        await _connectionService.StartConnectionAsync();
        await restarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _connectionService.StopConnection().WaitAsync(TimeSpan.FromSeconds(10));

        _mockConnectionFactory.Verify(f => f.BuildConnection(), Times.Exactly(2));
        _connectionService.ClientInstanceId.Should().Be(incompatible ? null : "instance-2");
    }

    [Test]
    public async Task FailedHubStop_PropagatesFailureBeforeAnIdentityCanBeChanged()
    {
        var hub = new Mock<TestableHubConnection> { CallBase = true };
        hub.Setup(h => h.StopAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("stop failed"));
        _mockAuthenticationTokensRepository.Setup(r => r.GetTokens()).ReturnsAsync((AuthenticationTokens?)null);
        _mockConnectionFactory.Setup(f => f.BuildConnection()).ReturnsAsync(new BuildConnectionResult
            { HubConnection = hub.Object, EndPoint = new ByteSyncEndpoint { ClientInstanceId = "old-instance" } });
        await _connectionService.StartConnectionAsync();

        var stop = () => _connectionService.StopConnection();

        await stop.Should().ThrowAsync<InvalidOperationException>();
        _connectionService.ClientInstanceId.Should().Be("old-instance");
    }

    [Test]
    public async Task StartConnection_TracksTheEntirePendingAuthenticationAttempt()
    {
        var result = new TaskCompletionSource<BuildConnectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _mockConnectionFactory.Setup(f => f.BuildConnection()).Returns(result.Task);
        _mockAuthenticationTokensRepository.Setup(r => r.GetTokens()).ReturnsAsync((AuthenticationTokens?)null);

        var start = _connectionService.StartConnectionAsync();
        _connectionService.IsStarting.Should().BeTrue();
        var duplicate = () => _connectionService.StartConnectionAsync();
        await duplicate.Should().ThrowAsync<InvalidOperationException>();
        result.SetResult(new BuildConnectionResult { HubConnection = new TestableHubConnection(), EndPoint = new ByteSyncEndpoint() });
        await start;

        _connectionService.IsStarting.Should().BeFalse();
        _mockConnectionFactory.Verify(f => f.BuildConnection(), Times.Once());
    }

    [Test]
    public async Task TokenRepository_ClearForcesSubsequentAuthenticationToUseFreshTokens()
    {
        var repository = new AuthenticationTokensRepository();
        await repository.Store(new AuthenticationTokens { JwtToken = "old-token", RefreshToken = "old-refresh" });

        await repository.Clear();

        (await repository.GetTokens()).Should().BeNull();
        await repository.Store(new AuthenticationTokens { JwtToken = "new-token" });
        (await repository.GetTokens())!.JwtToken.Should().Be("new-token");
    }

    [Test]
    public async Task MissingChallengeEndpoint_ClearsConnectionAndPublishesTerminalStatusWithoutRetrying()
    {
        var error = new RelayIncompatibleException(new ApiException("Not Found", System.Net.HttpStatusCode.NotFound));
        _mockConnectionFactory.SetupSequence(f => f.BuildConnection())
            .ThrowsAsync(error)
            .ThrowsAsync(new BuildConnectionException("Unexpected retry", InitialConnectionStatus.VersionNotAllowed));
        _connectionService.CurrentEndPoint = new ByteSyncEndpoint { ClientId = "stale", ClientInstanceId = "stale" };
        var statuses = new List<ConnectionStatuses>();
        using var subscription = _connectionService.ConnectionStatus.Subscribe(statuses.Add);
        Microsoft.AspNetCore.SignalR.Client.HubConnection? currentConnection = null;
        using var connectionSubscription = _connectionService.Connection.Subscribe(c => currentConnection = c);

        var act = () => _connectionService.StartConnectionAsync();

        (await act.Should().ThrowAsync<RelayIncompatibleException>()).Which.Should().BeSameAs(error);
        _mockConnectionFactory.Verify(f => f.BuildConnection(), Times.Once);
        statuses.Should().Equal(ConnectionStatuses.NotConnected, ConnectionStatuses.Connecting,
            ConnectionStatuses.NotConnected, ConnectionStatuses.RelayIncompatible);
        _connectionService.CurrentEndPoint.Should().BeNull();
        _connectionService.ClientInstanceId.Should().BeNull();
        currentConnection.Should().BeNull();
        _mockLogger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("auth/challenge") && state.ToString()!.Contains("Automatic reconnection has stopped")),
            error, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public async Task ManualStartAfterRelayUpgrade_CanConnectAgain()
    {
        _mockAuthenticationTokensRepository.Setup(r => r.GetTokens()).ReturnsAsync((AuthenticationTokens?)null);
        _mockConnectionFactory.SetupSequence(f => f.BuildConnection())
            .ThrowsAsync(new RelayIncompatibleException(new ApiException("Not Found")))
            .ReturnsAsync(new BuildConnectionResult
            {
                HubConnection = new TestableHubConnection(),
                EndPoint = new ByteSyncEndpoint { ClientId = "compatible", ClientInstanceId = "compatible" }
            });
        var initialStart = () => _connectionService.StartConnectionAsync();
        await initialStart.Should().ThrowAsync<RelayIncompatibleException>();

        await _connectionService.StartConnectionAsync();

        _connectionService.CurrentConnectionStatus.Should().Be(ConnectionStatuses.Connected);
        _connectionService.ClientInstanceId.Should().Be("compatible");
        _mockConnectionFactory.Verify(f => f.BuildConnection(), Times.Exactly(2));
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
