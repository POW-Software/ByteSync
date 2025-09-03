using ByteSync.Common.Business.Synchronizations;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Services;
using FakeItEasy;
using FluentAssertions;

namespace ByteSync.ServerCommon.Tests.Services;

[TestFixture]
public class SynchronizationServiceTests
{
    private ISynchronizationRepository _synchronizationRepository;
    private SynchronizationService _synchronizationService;

    [SetUp]
    public void Setup()
    {
        _synchronizationRepository = A.Fake<ISynchronizationRepository>(x => x.Strict());
        _synchronizationService = new SynchronizationService(_synchronizationRepository);
    }
    
    [Test]
    public void CheckSynchronizationIsFinished_AllMembersDoneAndAllActionsDone_SetsEndRegularAndReturnsTrue()
    {
        // Arrange
        var sync = new SynchronizationEntity
        {
            Progress = new SynchronizationProgressEntity
            {
                Members = new List<string> { "a", "b" },
                CompletedMembers = new List<string> { "a", "b" },
                TotalAtomicActionsCount = 10,
                FinishedAtomicActionsCount = 10
            }
        };

        // Act
        var updated = _synchronizationService.CheckSynchronizationIsFinished(sync);

        // Assert
        updated.Should().BeTrue();
        sync.IsEnded.Should().BeTrue();
        sync.EndedOn.Should().NotBeNull();
        sync.EndStatus.Should().Be(SynchronizationEndStatuses.Regular);
    }

    [Test]
    public void CheckSynchronizationIsFinished_AllMembersDoneAndAbortRequested_SetsEndAbortionAndReturnsTrue()
    {
        // Arrange
        var sync = new SynchronizationEntity
        {
            AbortRequestedOn = DateTimeOffset.UtcNow,
            Progress = new SynchronizationProgressEntity
            {
                Members = new List<string> { "a", "b" },
                CompletedMembers = new List<string> { "a", "b" },
                TotalAtomicActionsCount = 10,
                FinishedAtomicActionsCount = 5 // not all done, but abort requested
            }
        };

        // Act
        var updated = _synchronizationService.CheckSynchronizationIsFinished(sync);

        // Assert
        updated.Should().BeTrue();
        sync.IsEnded.Should().BeTrue();
        sync.EndStatus.Should().Be(SynchronizationEndStatuses.Abortion);
    }

    [Test]
    public void CheckSynchronizationIsFinished_NotAllMembersCompleted_ReturnsFalseAndDoesNotModify()
    {
        // Arrange
        var sync = new SynchronizationEntity
        {
            Progress = new SynchronizationProgressEntity
            {
                Members = new List<string> { "a", "b" },
                CompletedMembers = new List<string> { "a" },
                TotalAtomicActionsCount = 10,
                FinishedAtomicActionsCount = 10
            }
        };

        // Act
        var updated = _synchronizationService.CheckSynchronizationIsFinished(sync);

        // Assert
        updated.Should().BeFalse();
        sync.IsEnded.Should().BeFalse();
        sync.EndedOn.Should().BeNull();
        sync.EndStatus.Should().BeNull();
    }

    [Test]
    public void CheckSynchronizationIsFinished_AlreadyEnded_ReturnsFalseAndKeepsEndValues()
    {
        // Arrange
        var endedOn = DateTimeOffset.UtcNow.AddMinutes(-5);
        var sync = new SynchronizationEntity
        {
            EndedOn = endedOn,
            EndStatus = SynchronizationEndStatuses.Regular,
            Progress = new SynchronizationProgressEntity
            {
                Members = new List<string> { "a" },
                CompletedMembers = new List<string> { "a" },
                TotalAtomicActionsCount = 1,
                FinishedAtomicActionsCount = 1
            }
        };

        // Act
        var updated = _synchronizationService.CheckSynchronizationIsFinished(sync);

        // Assert
        updated.Should().BeFalse();
        sync.EndedOn.Should().Be(endedOn);
        sync.EndStatus.Should().Be(SynchronizationEndStatuses.Regular);
    }

    [Test]
    public async Task ResetSession_CallsRepositoryWithSessionId()
    {
        // Arrange
        var sessionId = "session-123";
        A.CallTo(() => _synchronizationRepository.ResetSession(sessionId))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationService.ResetSession(sessionId);

        // Assert
        A.CallTo(() => _synchronizationRepository.ResetSession(sessionId)).MustHaveHappenedOnceExactly();
    }
}
