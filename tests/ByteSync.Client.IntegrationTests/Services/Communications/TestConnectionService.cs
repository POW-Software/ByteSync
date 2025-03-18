using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using Autofac;
using ByteSync.Business.Communications;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
using ByteSync.Factories;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Services.Communications;
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
        var mockHubConnection = new TestableHubConnection();
        
        var endPoint = new ByteSyncEndpoint 
        { 
            ClientInstanceId = "test-instance-id",
            ClientId = "test-id"
        };

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
}