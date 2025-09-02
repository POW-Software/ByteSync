using ByteSync.Business.Profiles;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.Connecting;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ByteSync.Services.Sessions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Sessions.Connecting;

[TestFixture]
public class CreateSessionServiceTests
{
    private Mock<ICloudSessionConnectionRepository> _cloudSessionConnectionRepositoryMock = null!;
    private Mock<IDataEncrypter> _dataEncrypterMock = null!;
    private Mock<IEnvironmentService> _environmentServiceMock = null!;
    private Mock<ICloudSessionApiClient> _cloudSessionApiClientMock = null!;
    private Mock<IPublicKeysManager> _publicKeysManagerMock = null!;
    private Mock<ITrustProcessPublicKeysRepository> _trustProcessPublicKeysRepositoryMock = null!;
    private Mock<IDigitalSignaturesRepository> _digitalSignaturesRepositoryMock = null!;
    private Mock<IAfterJoinSessionService> _afterJoinSessionServiceMock = null!;
    private Mock<ICloudSessionConnectionService> _cloudSessionConnectionServiceMock = null!;
    private Mock<ILogger<CreateSessionService>> _loggerMock = null!;
    private CancellationTokenSource _cts = null!;

    private CreateSessionService _service;

    [SetUp]
    public void SetUp()
    {
        _cloudSessionConnectionRepositoryMock = new Mock<ICloudSessionConnectionRepository>();
        _dataEncrypterMock = new Mock<IDataEncrypter>();
        _environmentServiceMock = new Mock<IEnvironmentService>();
        _cloudSessionApiClientMock = new Mock<ICloudSessionApiClient>();
        _publicKeysManagerMock = new Mock<IPublicKeysManager>();
        _trustProcessPublicKeysRepositoryMock = new Mock<ITrustProcessPublicKeysRepository>();
        _digitalSignaturesRepositoryMock = new Mock<IDigitalSignaturesRepository>();
        _afterJoinSessionServiceMock = new Mock<IAfterJoinSessionService>();
        _cloudSessionConnectionServiceMock = new Mock<ICloudSessionConnectionService>();
        _loggerMock = new Mock<ILogger<CreateSessionService>>();

        _cts = new CancellationTokenSource();
        _cloudSessionConnectionRepositoryMock.SetupGet(x => x.CancellationToken).Returns(_cts.Token);
        _cloudSessionConnectionRepositoryMock.SetupGet(x => x.CancellationTokenSource).Returns(_cts);

        _service = new CreateSessionService(
            _cloudSessionConnectionRepositoryMock.Object,
            _dataEncrypterMock.Object,
            _environmentServiceMock.Object,
            _cloudSessionApiClientMock.Object,
            _publicKeysManagerMock.Object,
            _trustProcessPublicKeysRepositoryMock.Object,
            _digitalSignaturesRepositoryMock.Object,
            _afterJoinSessionServiceMock.Object,
            _cloudSessionConnectionServiceMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task CreateCloudSession_WithProfileInfo_ShouldReturnResultAndCallDependencies()
    {
        // Arrange
        var runCloudSessionProfileInfo = new RunCloudSessionProfileInfo("Lobby1", new CloudSessionProfile(),
            new CloudSessionProfileDetails(), LobbySessionModes.RunInventory);
        
        var request = new CreateCloudSessionRequest(runCloudSessionProfileInfo);
        
        var encryptedSettingsDefault = new EncryptedSessionSettings();
        var encryptedPrivateDataDefault = new EncryptedSessionMemberPrivateData();
        var publicKeyInfo = new PublicKeyInfo();
        
        _dataEncrypterMock.Setup(x => x.EncryptSessionSettings(It.IsAny<SessionSettings>()))
            .Returns(encryptedSettingsDefault);
        _dataEncrypterMock.Setup(x => x.EncryptSessionMemberPrivateData(It.IsAny<SessionMemberPrivateData>()))
            .Returns(encryptedPrivateDataDefault);
        _environmentServiceMock.Setup(x => x.MachineName)
            .Returns("TestMachine");
        _publicKeysManagerMock.Setup(x => x.GetMyPublicKeyInfo())
            .Returns(publicKeyInfo);

        var dummyResult = new CloudSessionResult()
        {
            CloudSession = new()
            {
                SessionId = "TestSession"
            }
        };

        CreateCloudSessionParameters capturedParameters = null;
        _cloudSessionApiClientMock
            .Setup(x => x.CreateCloudSession(It.IsAny<CreateCloudSessionParameters>(), It.IsAny<CancellationToken>()))
            .Callback<CreateCloudSessionParameters, CancellationToken>((p, token) => capturedParameters = p)
            .ReturnsAsync(dummyResult);

        _cloudSessionConnectionServiceMock
            .Setup(x => x.InitializeConnection(SessionConnectionStatus.CreatingSession))
            .Returns(Task.CompletedTask);
        _trustProcessPublicKeysRepositoryMock
            .Setup(x => x.Start(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _digitalSignaturesRepositoryMock
            .Setup(x => x.Start(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _afterJoinSessionServiceMock
            .Setup(x => x.Process(It.IsAny<AfterJoinSessionRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateCloudSession(request);

        // Assert
        result.Should().BeEquivalentTo(dummyResult);
        _cloudSessionConnectionRepositoryMock.Verify(x => x.SetConnectionStatus(SessionConnectionStatus.InSession), Times.Once);
        _trustProcessPublicKeysRepositoryMock.Verify(x => x.Start("TestSession"), Times.Once);
        _digitalSignaturesRepositoryMock.Verify(x => x.Start("TestSession"), Times.Once);
        _afterJoinSessionServiceMock.Verify(x => x.Process(It.Is<AfterJoinSessionRequest>(req =>
            req.CloudSessionResult == dummyResult &&
            req.RunCloudSessionProfileInfo == request.RunCloudSessionProfileInfo &&
            req.IsCreator == true)), Times.Once);

        capturedParameters.Should().NotBeNull();
        capturedParameters.LobbyId.Should().Be(request.RunCloudSessionProfileInfo.LobbyId);
        capturedParameters.CreatorProfileClientId.Should().Be(request.RunCloudSessionProfileInfo.LocalProfileClientId);
        capturedParameters.SessionSettings.Should().Be(encryptedSettingsDefault);
        capturedParameters.CreatorPublicKeyInfo.Should().Be(publicKeyInfo);
        capturedParameters.CreatorPrivateData.Should().Be(encryptedPrivateDataDefault);

        // Vérification du log (utilisation de Verify sur la méthode Log générique)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Created Cloud Session") &&
                                              v.ToString().Contains("TestSession")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task CreateCloudSession_WithNullProfileInfo_ShouldUseDefaultSessionSettings()
    {
        // Arrange
        var request = new CreateCloudSessionRequest(null);

        var encryptedSettingsDefault = new EncryptedSessionSettings();
        var encryptedPrivateDataDefault = new EncryptedSessionMemberPrivateData();
        var publicKeyInfo = new PublicKeyInfo();
        
        _dataEncrypterMock.Setup(x => x.EncryptSessionSettings(It.IsAny<SessionSettings>()))
            .Returns(encryptedSettingsDefault);
        _dataEncrypterMock.Setup(x => x.EncryptSessionMemberPrivateData(It.IsAny<SessionMemberPrivateData>()))
            .Returns(encryptedPrivateDataDefault);
        _environmentServiceMock.Setup(x => x.MachineName)
            .Returns("TestMachine");
        _publicKeysManagerMock.Setup(x => x.GetMyPublicKeyInfo())
            .Returns(publicKeyInfo);

        var dummyResult = new CloudSessionResult()
        {
            CloudSession = new()
            {
                SessionId = "TestSession"
            }
        };

        CreateCloudSessionParameters capturedParameters = null;
        _cloudSessionApiClientMock
            .Setup(x => x.CreateCloudSession(It.IsAny<CreateCloudSessionParameters>(), It.IsAny<CancellationToken>()))
            .Callback<CreateCloudSessionParameters, CancellationToken>((p, token) => capturedParameters = p)
            .ReturnsAsync(dummyResult);

        _cloudSessionConnectionServiceMock
            .Setup(x => x.InitializeConnection(SessionConnectionStatus.CreatingSession))
            .Returns(Task.CompletedTask);
        _trustProcessPublicKeysRepositoryMock
            .Setup(x => x.Start(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _digitalSignaturesRepositoryMock
            .Setup(x => x.Start(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _afterJoinSessionServiceMock
            .Setup(x => x.Process(It.IsAny<AfterJoinSessionRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateCloudSession(request);

        // Assert
        result.Should().BeEquivalentTo(dummyResult);
        capturedParameters.Should().NotBeNull();
        capturedParameters.LobbyId.Should().BeNull();
        capturedParameters.CreatorProfileClientId.Should().BeNull();
        capturedParameters.SessionSettings.Should().Be(encryptedSettingsDefault);
        capturedParameters.CreatorPublicKeyInfo.Should().Be(publicKeyInfo);
        capturedParameters.CreatorPrivateData.Should().Be(encryptedPrivateDataDefault);
    }

    [Test]
    public async Task CreateCloudSession_ShouldThrowTaskCanceledException_WhenCancellationRequested()
    {
        // Arrange
        var runCloudSessionProfileInfo = new RunCloudSessionProfileInfo("Lobby1", new CloudSessionProfile(),
            new CloudSessionProfileDetails(), LobbySessionModes.RunInventory);
        
        var request = new CreateCloudSessionRequest(runCloudSessionProfileInfo);

        // We simulate the cancellation by canceling the CancellationTokenSource
        _cts.Cancel();

        var dummyResult = new CloudSessionResult()
        {
            CloudSession = new()
            {
                SessionId = "TestSession"
            }
        };

        _cloudSessionApiClientMock
            .Setup(x => x.CreateCloudSession(It.IsAny<CreateCloudSessionParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyResult);

        // Act
        await _service.CreateCloudSession(request);

        // Assert
        _cloudSessionConnectionServiceMock.Verify(x => x.OnCreateSessionError(
                It.Is<CreateSessionError>(err =>
                    err.Exception is TaskCanceledException &&
                    err.Status == CreateSessionStatus.CanceledByUser)),
            Times.Once);
    }

    [Test]
    public async Task CreateCloudSession_ShouldThrowException_WhenCloudSessionApiClientFails()
    {
        // Arrange
        var runCloudSessionProfileInfo = new RunCloudSessionProfileInfo("Lobby1", new CloudSessionProfile(),
            new CloudSessionProfileDetails(), LobbySessionModes.RunInventory);
        
        var request = new CreateCloudSessionRequest(runCloudSessionProfileInfo);

        var exception = new InvalidOperationException("Test exception");
        _cloudSessionApiClientMock
            .Setup(x => x.CreateCloudSession(It.IsAny<CreateCloudSessionParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await _service.CreateCloudSession(request);

        // Assert
        _cloudSessionConnectionServiceMock.Verify(x => x.OnCreateSessionError(
                It.Is<CreateSessionError>(err =>
                    err.Exception == exception &&
                    err.Status == CreateSessionStatus.Error)),
            Times.Once);
    }

    [Test]
    public async Task CancelCreateCloudSession_ShouldCancelToken()
    {
        // Act
        await _service.CancelCreateCloudSession();

        // Assert: the CancellationTokenSource must be canceled
        _cts.IsCancellationRequested.Should().BeTrue();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("User requested to cancel Cloud Session creation")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}