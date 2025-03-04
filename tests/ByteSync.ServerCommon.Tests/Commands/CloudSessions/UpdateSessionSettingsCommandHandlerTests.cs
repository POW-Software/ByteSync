using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.CloudSessions;
using ByteSync.ServerCommon.Exceptions;
using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Mappers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Tests.Helpers;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Commands.CloudSessions;

[TestFixture]
public class UpdateSessionSettingsCommandHandlerTests
{
    private ICloudSessionsRepository _mockCloudSessionsRepository;
    private IInventoryRepository _mockInventoryRepository;
    private ISynchronizationRepository _mockSynchronizationRepository;
    private ICacheService _mockCacheService;
    private ISessionMemberMapper _mockSessionMemberMapper;
    private IByteSyncClientCaller _mockByteSyncClientCaller;
    private ILogger<UpdateSessionSettingsCommandHandler> _mockLogger;
    private UpdateSessionSettingsCommandHandler _updateSessionSettingsCommandHandler;

    [SetUp]
    public void Setup()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockSynchronizationRepository = A.Fake<ISynchronizationRepository>();
        _mockCacheService = A.Fake<ICacheService>();
        _mockSessionMemberMapper = A.Fake<ISessionMemberMapper>();
        _mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>();
        _mockLogger = A.Fake<ILogger<UpdateSessionSettingsCommandHandler>>();

        _updateSessionSettingsCommandHandler = new UpdateSessionSettingsCommandHandler(
            _mockCloudSessionsRepository,
            _mockInventoryRepository,
            _mockSynchronizationRepository,
            _mockCacheService,
            _mockSessionMemberMapper,
            _mockByteSyncClientCaller,
            _mockLogger);
    }

    [Test]
    public async Task Handle_WithNullSettings_ThrowsBadRequestException()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientInstanceId = "clientInstance1" };
        var request = new UpdateSessionSettingsRequest(sessionId, client, null);

        // Act & Assert
        await FluentActions.Invoking(() => _updateSessionSettingsCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>()
            .WithMessage("UpdateSessionSettings: sessionSettings null");
    }

    [Test]
    public async Task Handle_WithValidSettingsAndSessionNotActivated_UpdatesSettingsAndNotifiesOthers()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientInstanceId = "clientInstance1" };
        var settings = new EncryptedSessionSettings();
        var cloudSessionData = new CloudSessionData { IsSessionActivated = false };

        bool funcResult = true;
        bool isTransaction = false;
        A.CallTo(() => _mockCloudSessionsRepository.Update(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, 
                A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction? transaction, IRedLock? _) =>
            {
                funcResult = func(cloudSessionData);
                isTransaction = transaction != null;
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildUpdateResult(funcResult, cloudSessionData, isTransaction));
            // .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.Saved));

        var mockGroupExcept = A.Fake<IHubByteSyncPush>();
        A.CallTo(() => _mockByteSyncClientCaller.SessionGroupExcept(sessionId, client)).Returns(mockGroupExcept);

        // Act
        var request = new UpdateSessionSettingsRequest(sessionId, client, settings);
        var result = await _updateSessionSettingsCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        cloudSessionData.SessionSettings.Should().BeSameAs(settings);

        A.CallTo(() => _mockCloudSessionsRepository.Update(sessionId, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mockByteSyncClientCaller.SessionGroupExcept(sessionId, client))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => mockGroupExcept.SessionSettingsUpdated(A<SessionSettingsUpdatedDTO>.That.Matches(dto =>
            dto.SessionId == sessionId &&
            dto.ClientInstanceId == client.ClientInstanceId &&
            dto.EncryptedSessionSettings == settings)))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WithSessionAlreadyActivated_ReturnsFalseWithoutUpdating()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientInstanceId = "clientInstance1" };
        var settings = new EncryptedSessionSettings();
        var cloudSessionData = new CloudSessionData { IsSessionActivated = true };

        bool funcResult = true;
        bool isTransaction = false;
        A.CallTo(() => _mockCloudSessionsRepository.Update(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction? transaction, IRedLock? _) =>
            {
                funcResult = func(cloudSessionData);
                isTransaction = transaction != null;
            })
            // .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.NoOperation));
            .ReturnsLazily(() => UpdateResultBuilder.BuildUpdateResult(funcResult, cloudSessionData, isTransaction));

        // Act
        var request = new UpdateSessionSettingsRequest(sessionId, client, settings);
        var result = await _updateSessionSettingsCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        A.CallTo(() => _mockCloudSessionsRepository.Update(sessionId, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mockByteSyncClientCaller.SessionGroupExcept(A<string>.Ignored, A<Client>.Ignored))
            .MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenSessionNotFound_ReturnsFalse()
    {
        // Arrange
        var sessionId = "nonExistingSession";
        var client = new Client { ClientInstanceId = "clientInstance1" };
        var settings = new EncryptedSessionSettings();

        A.CallTo(() => _mockCloudSessionsRepository.Update(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Returns(new UpdateEntityResult<CloudSessionData>(null, UpdateEntityStatus.NotFound));

        // Act
        var request = new UpdateSessionSettingsRequest(sessionId, client, settings);
        var result = await _updateSessionSettingsCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        A.CallTo(() => _mockCloudSessionsRepository.Update(sessionId, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mockByteSyncClientCaller.SessionGroupExcept(A<string>.Ignored, A<Client>.Ignored))
            .MustNotHaveHappened();
    }
}