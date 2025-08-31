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
public class DateIsCopiedCommandHandlerTests
{
    private ITrackingActionRepository _mockTrackingActionRepository;
    private ISynchronizationStatusCheckerService _mockSynchronizationStatusCheckerService;
    private ISynchronizationProgressService _mockSynchronizationProgressService;
    private ISynchronizationService _mockSynchronizationService;
    private ILogger<DateIsCopiedCommandHandler> _mockLogger;
    private DateIsCopiedCommandHandler _dateIsCopiedCommandHandler;

    [SetUp]
    public void Setup()
    {
        _mockTrackingActionRepository = A.Fake<ITrackingActionRepository>();
        _mockSynchronizationStatusCheckerService = A.Fake<ISynchronizationStatusCheckerService>();
        _mockSynchronizationProgressService = A.Fake<ISynchronizationProgressService>();
        _mockSynchronizationService = A.Fake<ISynchronizationService>();
        _mockLogger = A.Fake<ILogger<DateIsCopiedCommandHandler>>();

        _dateIsCopiedCommandHandler = new DateIsCopiedCommandHandler(
            _mockTrackingActionRepository,
            _mockSynchronizationStatusCheckerService,
            _mockSynchronizationProgressService,
            _mockSynchronizationService,
            _mockLogger);
    }

    [Test]
    public async Task Handle_ValidRequest_ProcessesDateIsCopied()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var actionsGroupIds = new List<string> { "group1", "group2" };

        var request = new DateIsCopiedRequest(sessionId, client, actionsGroupIds, "testNodeId");

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
        await _dateIsCopiedCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, actionsGroupIds, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(A<TrackingActionResult>._, A<bool>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WithEmptyActionsGroupIds_ProcessesDateIsCopied()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var actionsGroupIds = new List<string>();

        var request = new DateIsCopiedRequest(sessionId, client, actionsGroupIds, "testNodeId");

        // Act
        await _dateIsCopiedCommandHandler.Handle(request, CancellationToken.None);

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

        var request = new DateIsCopiedRequest(sessionId, client, actionsGroupIds, "testNodeId");
        var expectedException = new InvalidOperationException("Test exception");

        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, actionsGroupIds, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>._))
            .Throws(expectedException);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => 
            _dateIsCopiedCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().Be(expectedException);
    }

    [Test]
    public async Task Handle_WithNodeIdNull_ShouldNotThrowException()
    {
        // Arrange
        const string sessionId = "session123";
        var client = new Client { ClientInstanceId = "client1" };
        var actionsGroupIds = new List<string> { "action1" };
        
        // Request with NodeId = null
        var request = new DateIsCopiedRequest(sessionId, client, actionsGroupIds, null);

        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, actionsGroupIds, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>._))
            .Invokes((string _, List<string> _, Func<TrackingActionEntity, SynchronizationEntity, bool> func) => 
            {
                var trackingAction = new TrackingActionEntity
                {
                    // Client1 a plusieurs NodeIds, Client2 en a un autre
                    TargetClientInstanceAndNodeIds =
                    [
                        new() { ClientInstanceId = "client1", NodeId = "node1" },
                        new() { ClientInstanceId = "client1", NodeId = "node2" },
                        new() { ClientInstanceId = "client2", NodeId = "node3" }
                    ]
                };
                
                var synchronization = new SynchronizationEntity
                {
                    Progress = new SynchronizationProgressEntity()
                };
                
                // Cette fonction devrait maintenant réussir au lieu de lancer une exception
                var result = func(trackingAction, synchronization);
                result.Should().BeTrue(); // Vérifie que l'opération a réussi
            })
            .Returns(new TrackingActionResult(true, [], new SynchronizationEntity()));

        A.CallTo(() => _mockSynchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(A<SynchronizationEntity>._))
            .Returns(true);

        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(A<TrackingActionResult>._, A<bool>._))
            .Returns(Task.CompletedTask);

        // Act & Assert
        // Cette opération ne doit plus lancer d'exception avec NodeId = null
        await FluentActions.Awaiting(() => 
            _dateIsCopiedCommandHandler.Handle(request, CancellationToken.None))
            .Should().NotThrowAsync();
    }
}