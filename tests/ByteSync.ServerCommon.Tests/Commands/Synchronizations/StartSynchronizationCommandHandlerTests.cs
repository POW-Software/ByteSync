using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Synchronizations;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.Synchronizations;

[TestFixture]
public class StartSynchronizationCommandHandlerTests
{
    private ICloudSessionsRepository _mockCloudSessionsRepository;
    private ISynchronizationRepository _mockSynchronizationRepository;
    private ISynchronizationProgressService _mockSynchronizationProgressService;
    private ILogger<StartSynchronizationCommandHandler> _mockLogger;
    private StartSynchronizationCommandHandler _startSynchronizationCommandHandler;

    [SetUp]
    public void Setup()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockSynchronizationRepository = A.Fake<ISynchronizationRepository>();
        _mockSynchronizationProgressService = A.Fake<ISynchronizationProgressService>();
        _mockLogger = A.Fake<ILogger<StartSynchronizationCommandHandler>>();

        _startSynchronizationCommandHandler = new StartSynchronizationCommandHandler(
            _mockCloudSessionsRepository,
            _mockSynchronizationRepository,
            _mockSynchronizationProgressService,
            _mockLogger);
    }

    [Test]
    public async Task Handle_ValidRequest_StartsSynchronization()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var actionsGroupDefinitions = new List<ActionsGroupDefinition>
        {
            new() { ActionsGroupId = "group1" },
            new() { ActionsGroupId = "group2" }
        };

        var request = new StartSynchronizationRequest(sessionId, client, actionsGroupDefinitions);
        var session = new CloudSessionData
        {
            SessionMembers =
            [
                new() { ClientInstanceId = "client1" },
                new() { ClientInstanceId = "client2" }
            ]
        };

        A.CallTo(() => _mockSynchronizationRepository.Get(sessionId))
            .Returns((SynchronizationEntity?)null);
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(session);
        A.CallTo(() => _mockSynchronizationRepository.AddSynchronization(A<SynchronizationEntity>._, actionsGroupDefinitions))
            .Returns(Task.CompletedTask);
        A.CallTo(() => _mockSynchronizationProgressService.InformSynchronizationStarted(A<SynchronizationEntity>._, client))
            .Returns(Task.CompletedTask);

        // Act
        await _startSynchronizationCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockSynchronizationRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSynchronizationRepository.AddSynchronization(A<SynchronizationEntity>._, actionsGroupDefinitions)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSynchronizationProgressService.InformSynchronizationStarted(A<SynchronizationEntity>._, client)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WithEmptyActionsGroupDefinitions_StartsSynchronization()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var actionsGroupDefinitions = new List<ActionsGroupDefinition>();

        var request = new StartSynchronizationRequest(sessionId, client, actionsGroupDefinitions);
        var session = new CloudSessionData
        {
            SessionMembers = [new() { ClientInstanceId = "client1" }]
        };

        A.CallTo(() => _mockSynchronizationRepository.Get(sessionId))
            .Returns((SynchronizationEntity?)null);
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(session);
        A.CallTo(() => _mockSynchronizationRepository.AddSynchronization(A<SynchronizationEntity>._, actionsGroupDefinitions))
            .Returns(Task.CompletedTask);
        A.CallTo(() => _mockSynchronizationProgressService.InformSynchronizationStarted(A<SynchronizationEntity>._, client))
            .Returns(Task.CompletedTask);

        // Act
        await _startSynchronizationCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockSynchronizationRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSynchronizationRepository.AddSynchronization(A<SynchronizationEntity>._, actionsGroupDefinitions)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSynchronizationProgressService.InformSynchronizationStarted(A<SynchronizationEntity>._, client)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WhenSynchronizationExists_DoesNotCreateNewSynchronization()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var actionsGroupDefinitions = new List<ActionsGroupDefinition>();

        var request = new StartSynchronizationRequest(sessionId, client, actionsGroupDefinitions);
        var existingSynchronization = new SynchronizationEntity();

        A.CallTo(() => _mockSynchronizationRepository.Get(sessionId))
            .Returns(existingSynchronization);

        // Act
        await _startSynchronizationCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockSynchronizationRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockCloudSessionsRepository.Get(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => _mockSynchronizationRepository.AddSynchronization(A<SynchronizationEntity>._, A<List<ActionsGroupDefinition>>._)).MustNotHaveHappened();
        A.CallTo(() => _mockSynchronizationProgressService.InformSynchronizationStarted(A<SynchronizationEntity>._, A<Client>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var actionsGroupDefinitions = new List<ActionsGroupDefinition>();

        var request = new StartSynchronizationRequest(sessionId, client, actionsGroupDefinitions);
        var expectedException = new InvalidOperationException("Test exception");

        A.CallTo(() => _mockSynchronizationRepository.Get(sessionId))
            .Throws(expectedException);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => 
            _startSynchronizationCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().Be(expectedException);
    }
}