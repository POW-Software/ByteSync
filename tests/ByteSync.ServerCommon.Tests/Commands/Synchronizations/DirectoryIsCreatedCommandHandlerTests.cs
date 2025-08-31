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
public class DirectoryIsCreatedCommandHandlerTests
{
    private ITrackingActionRepository _mockTrackingActionRepository;
    private ISynchronizationStatusCheckerService _mockSynchronizationStatusCheckerService;
    private ISynchronizationProgressService _mockSynchronizationProgressService;
    private ISynchronizationService _mockSynchronizationService;
    private ILogger<DirectoryIsCreatedCommandHandler> _mockLogger;
    private DirectoryIsCreatedCommandHandler _directoryIsCreatedCommandHandler;

    [SetUp]
    public void Setup()
    {
        _mockTrackingActionRepository = A.Fake<ITrackingActionRepository>();
        _mockSynchronizationStatusCheckerService = A.Fake<ISynchronizationStatusCheckerService>();
        _mockSynchronizationProgressService = A.Fake<ISynchronizationProgressService>();
        _mockSynchronizationService = A.Fake<ISynchronizationService>();
        _mockLogger = A.Fake<ILogger<DirectoryIsCreatedCommandHandler>>();

        _directoryIsCreatedCommandHandler = new DirectoryIsCreatedCommandHandler(
            _mockTrackingActionRepository,
            _mockSynchronizationStatusCheckerService,
            _mockSynchronizationProgressService,
            _mockSynchronizationService,
            _mockLogger);
    }

    [Test]
    public async Task Handle_ValidRequest_ProcessesDirectoryIsCreated()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var actionsGroupIds = new List<string> { "group1", "group2" };

        var request = new DirectoryIsCreatedRequest(sessionId, client, actionsGroupIds, "testNodeId");

        A.CallTo(() => _mockSynchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(A<SynchronizationEntity>._))
            .Returns(true);
        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, actionsGroupIds, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>._))
            .Invokes((string _, List<string> _, Func<TrackingActionEntity, SynchronizationEntity, bool> func) => 
            {
                var trackingAction = new TrackingActionEntity
                {
                    TargetClientInstanceAndNodeIds =
                    [
                        new() { ClientInstanceId = "client1", NodeId = "testNodeId" },
                        new() { ClientInstanceId = "client2", NodeId = "testNodeId" }
                    ]
                };
                var synchronization = new SynchronizationEntity
                {
                    Progress = new SynchronizationProgressEntity()
                };
                func(trackingAction, synchronization);
            })
            .Returns(new TrackingActionResult(true, [], new SynchronizationEntity()));
        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(A<TrackingActionResult>._, A<bool>._))
            .Returns(Task.CompletedTask);

        // Act
        await _directoryIsCreatedCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, actionsGroupIds, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(A<TrackingActionResult>._, A<bool>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WithEmptyActionsGroupIds_ProcessesDirectoryIsCreated()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var actionsGroupIds = new List<string>();

        var request = new DirectoryIsCreatedRequest(sessionId, client, actionsGroupIds, "testNodeId");

        // Act
        await _directoryIsCreatedCommandHandler.Handle(request, CancellationToken.None);

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
        var actionsGroupIds = new List<string> { "group1" };

        var request = new DirectoryIsCreatedRequest(sessionId, client, actionsGroupIds, "testNodeId");
        var expectedException = new InvalidOperationException("Test exception");

        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, actionsGroupIds, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>._))
            .Throws(expectedException);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => 
            _directoryIsCreatedCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().Be(expectedException);
    }
}