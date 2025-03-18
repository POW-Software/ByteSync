using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using Autofac;
using ByteSync.Business.Communications;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
using ByteSync.Exceptions;
using ByteSync.Factories;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Services.Communications;
using ByteSync.Services.Inventories;
using ByteSync.TestsCommon;
using FluentAssertions;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Moq;
using IConnectionFactory = ByteSync.Interfaces.Factories.IConnectionFactory;

namespace ByteSync.Client.IntegrationTests.Services.Communications;

public class TestConnectionService : IntegrationTest
{
    private ConnectionService _connectionService;

    [SetUp]
    public void SetUp()
    {
        RegisterType<ConnectionFactory, IConnectionFactory>();
        RegisterType<AuthenticationTokensRepository, IAuthenticationTokensRepository>();
        RegisterType<ConnectionService>();
        BuildMoqContainer();
        
        

        var contextHelper = new TestContextGenerator(Container);

        // _sessionId = contextHelper.GenerateSession();
        // _currentEndPoint = contextHelper.GenerateCurrentEndpoint();
        
        var mockEnvironmentService = Container.Resolve<Mock<IEnvironmentService>>();
        mockEnvironmentService.Setup(m => m.ClientId).Returns("test-client-id");
        mockEnvironmentService.Setup(m => m.ClientInstanceId).Returns("test-client-instance-id");
        mockEnvironmentService.Setup(m => m.OSPlatform).Returns(OSPlatforms.Windows);
        mockEnvironmentService.Setup(m => m.ApplicationVersion).Returns(new Version(2020, 1, 1));

        _connectionService = Container.Resolve<ConnectionService>();
    }

    [Test]
    public async Task StartConnectionAsync_SuccessfulConnection_ShouldUpdateConnectionStatus()
    {
        // Arrange
        // var mockHubConnection = new Mock<HubConnection>(MockBehavior.Loose); // new Mock<HubConnection>(MockBehavior.Strict, new Uri("http://test"), null);

        var mockHubConnection = new TestableHubConnection();
        
        var endPoint = new ByteSyncEndpoint 
        { 
            ClientInstanceId = "test-instance-id",
            ClientId = "test-id"
        };
        
        // var connectionResult = new ConnectionResult 
        // { 
        //     HubConnection = mockHubConnection.Object,
        //     EndPoint = endPoint,
        //     AuthenticateResponseStatus = InitialConnectionStatus.Success
        // };
        
        // _mockConnectionFactory.Setup(f => f.BuildConnection())
        //     .ReturnsAsync(connectionResult);

        var initialAuthenticationResponse = new InitialAuthenticationResponse
        {
            AuthenticationTokens = new AuthenticationTokens(),
            EndPoint = endPoint,
            InitialConnectionStatus = InitialConnectionStatus.Success
        };
        
        var mockAuthApiClient = Container.Resolve<Mock<IAuthApiClient>>();
        mockAuthApiClient.Setup(m => m.Login(It.IsAny<LoginData>())).ReturnsAsync(initialAuthenticationResponse);
        
        var mockHebConnectionFactory = Container.Resolve<Mock<IHubConnectionFactory>>();
        mockHebConnectionFactory.Setup(m => m.BuildConnection()).ReturnsAsync(mockHubConnection);

        ConnectionStatuses capturedStatus = ConnectionStatuses.NotConnected;
        HubConnection capturedConnection = null;
        ByteSyncEndpoint capturedEndPoint = null;

        // Subscribe to observe state changes
        using var statusSubscription = _connectionService.ConnectionStatus.Subscribe(status => capturedStatus = status);
        using var connectionSubscription = _connectionService.Connection.Subscribe(conn => capturedConnection = conn);

        // Act
        await _connectionService.StartConnectionAsync();

        // Assert
        capturedStatus.Should().Be(ConnectionStatuses.Connected);
        capturedConnection.Should().Be(mockHubConnection);
        _connectionService.CurrentEndPoint.Should().Be(endPoint);
        _connectionService.ClientInstanceId.Should().Be(endPoint.ClientInstanceId);
    }

    /*
    [Test]
    public async Task StartConnectionAsync_VersionNotAllowed_ShouldThrowException()
    {
        // Arrange
        var connectionResult = new ConnectionResult 
        { 
            HubConnection = null,
            EndPoint = null,
            AuthenticateResponseStatus = InitialConnectionStatus.VersionNotAllowed
        };
        
        _mockConnectionFactory.Setup(f => f.BuildConnection())
            .ReturnsAsync(connectionResult);

        // Act & Assert
        await FluentActions.Invoking(async () => await _connectionService.StartConnectionAsync())
            .Should()
            .ThrowAsync<BuildConnectionException>()
            .WithMessage("*VersionNotAllowed*");

        _connectionService.CurrentConnectionStatus.Should().Be(ConnectionStatuses.NotConnected);
    }

    [Test]
    public async Task StartConnectionAsync_ConnectionFailure_ShouldRetry()
    {
        // Arrange
        int attemptCount = 0;
        _mockConnectionFactory.Setup(f => f.BuildConnection())
            .ReturnsAsync(() =>
            {
                attemptCount++;
                if (attemptCount == 1)
                {
                    throw new Exception("Connection failure");
                }
                
                var mockHubConnection = new Mock<HubConnection>(MockBehavior.Strict, new Uri("http://test"), null);
                return new ConnectionResult
                {
                    HubConnection = mockHubConnection.Object,
                    EndPoint = new ByteSyncEndpoint { ClientInstanceId = "test-instance-id" },
                    AuthenticateResponseStatus = InitialConnectionStatus.Success
                };
            });

        // Act
        await _connectionService.StartConnectionAsync();

        // Assert
        attemptCount.Should().Be(2);
        _connectionService.CurrentConnectionStatus.Should().Be(ConnectionStatuses.Connected);
    }
    */
    
    // public class TestableHubConnection : HubConnection
    // {
    //     public TestableHubConnection() : base(new MockConnectionContext(), new MockProtocol(), new MockEndPoint(), new MockServiceProvider(), new MockLoggerFactory())
    //     {
    //     }
    //
    //     // Implement essential methods needed by your tests
    //     public override Task StartAsync(CancellationToken cancellationToken = default)
    //     {
    //         return Task.CompletedTask;
    //     }
    //
    //     public override Task StopAsync(CancellationToken cancellationToken = default)
    //     {
    //         return Task.CompletedTask;
    //     }
    // }
    //
    // // Simple mock implementations for required dependencies
    // public class MockConnectionContext { }
    // public class MockProtocol { }
    // public class MockEndPoint { }
    // public class MockServiceProvider : IServiceProvider
    // {
    //     public object GetService(Type serviceType) => null;
    // }
    // public class MockLoggerFactory { }
    
    // Test implementation of HubConnection that doesn't need constructor parameters
public class TestableHubConnection : HubConnection
{
    private static readonly TestConnectionContext ConnectionContext = new();
    private static readonly TestHubProtocol Protocol = new();
    private static readonly TestEndPoint EndPoint = new();
    private static readonly TestServiceProvider ServiceProvider = new();
    private static readonly TestLoggerFactory LoggerFactory = new();

    public TestableHubConnection()
        : base(new TestConnectionFactory(), Protocol, EndPoint, ServiceProvider, LoggerFactory)
    {
    }

    // Mock implementations to provide constructor parameters
    private class TestConnectionFactory : Microsoft.AspNetCore.Connections.IConnectionFactory
    {
        public ValueTask<ConnectionContext> ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<ConnectionContext>(ConnectionContext);
        }
    }

    private class TestConnectionContext : ConnectionContext
    {
        public override string ConnectionId { get; set; }
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override IDictionary<object, object?> Items { get; set; }
        public override IDuplexPipe Transport { get; set; } = null!;
    }

    private class TestHubProtocol : IHubProtocol
    {
        public string Name => "test-protocol";
        public int Version => 1;
        public TransferFormat TransferFormat => TransferFormat.Binary;

        public bool IsVersionSupported(int version) => true;
        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message) => new byte[0];
        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            message = null!;
            return false;
        }
        public void WriteMessage(HubMessage message, IBufferWriter<byte> output) { }
    }

    private class TestEndPoint : EndPoint { }

    private class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private class TestLoggerFactory : ILoggerFactory
    {
        public void Dispose() { }
        public ILogger CreateLogger(string categoryName) => new TestLogger();
        public void AddProvider(ILoggerProvider provider) { }
    }

    private class TestLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new TestDisposable();
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    private class TestDisposable : IDisposable
    {
        public void Dispose() { }
    }

    // Override the abstract/virtual methods we need
    public override Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public override Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
}