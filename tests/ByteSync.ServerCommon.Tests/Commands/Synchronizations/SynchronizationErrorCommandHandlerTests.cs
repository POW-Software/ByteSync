using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Commands.Synchronizations;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.Synchronizations;

[TestFixture]
public class SynchronizationErrorCommandHandlerTests
{
    private ITrackingActionRepository _mockTrackingActionRepository;
    private ISynchronizationStatusCheckerService _mockSynchronizationStatusCheckerService;
    private ISynchronizationProgressService _mockSynchronizationProgressService;
    private ISynchronizationService _mockSynchronizationService;
    private ILogger<SynchronizationErrorCommandHandler> _mockLogger;
    private SynchronizationErrorCommandHandler _synchronizationErrorCommandHandler;

    [SetUp]
    public void Setup()
    {
        _mockTrackingActionRepository = A.Fake<ITrackingActionRepository>();
        _mockSynchronizationStatusCheckerService = A.Fake<ISynchronizationStatusCheckerService>();
        _mockSynchronizationProgressService = A.Fake<ISynchronizationProgressService>();
        _mockSynchronizationService = A.Fake<ISynchronizationService>();
        _mockLogger = A.Fake<ILogger<SynchronizationErrorCommandHandler>>();

        _synchronizationErrorCommandHandler = new SynchronizationErrorCommandHandler(
            _mockTrackingActionRepository,
            _mockSynchronizationStatusCheckerService,
            _mockSynchronizationProgressService,
            _mockSynchronizationService,
            _mockLogger);
    }

    [Test]
    public async Task Handle_ValidRequest_ProcessesSynchronizationError()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition
        {
            ActionsGroupIds = new List<string> { "group1", "group2" }
        };

        var request = new SynchronizationErrorRequest(sessionId, client, sharedFileDefinition);

        A.CallTo(() => _mockSynchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(A<SynchronizationEntity>._))
            .Returns(true);
        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, sharedFileDefinition.ActionsGroupIds!, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>._))
            .Invokes((string _, List<string> _, Func<TrackingActionEntity, SynchronizationEntity, bool> func) => 
            {
                var trackingAction = new TrackingActionEntity
                {
                    SourceClientInstanceId = "sourceClient",
                    TargetClientInstanceAndNodeIds = new HashSet<string> { "client1_node1", "client2_node2" }
                };
                var synchronization = new SynchronizationEntity
                {
                    Progress = new SynchronizationProgressEntity()
                };
                func(trackingAction, synchronization);
            })
            .Returns(new TrackingActionResult(true, new List<TrackingActionEntity>(), new SynchronizationEntity()));
        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(A<TrackingActionResult>._, A<bool>._))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationErrorCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, sharedFileDefinition.ActionsGroupIds!, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(A<TrackingActionResult>._, A<bool>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WithEmptyActionsGroupIds_ProcessesSynchronizationError()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition
        {
            ActionsGroupIds = new List<string>()
        };

        var request = new SynchronizationErrorRequest(sessionId, client, sharedFileDefinition);

        // Act
        await _synchronizationErrorCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(A<string>._, A<List<string>>._, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(A<TrackingActionResult>._, A<bool>._))
            .MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition
        {
            ActionsGroupIds = new List<string> { "group1" }
        };

        var request = new SynchronizationErrorRequest(sessionId, client, sharedFileDefinition);
        var expectedException = new InvalidOperationException("Test exception");

        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, sharedFileDefinition.ActionsGroupIds!, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>._))
            .Throws(expectedException);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => 
            _synchronizationErrorCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().Be(expectedException);
    }
}