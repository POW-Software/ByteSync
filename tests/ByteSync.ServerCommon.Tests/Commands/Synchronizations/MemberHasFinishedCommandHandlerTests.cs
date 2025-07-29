using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Commands.Synchronizations;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Commands.Synchronizations;

[TestFixture]
public class MemberHasFinishedCommandHandlerTests
{
    private ISynchronizationRepository _mockSynchronizationRepository;
    private ISynchronizationStatusCheckerService _mockSynchronizationStatusCheckerService;
    private ISynchronizationProgressService _mockSynchronizationProgressService;
    private ILogger<MemberHasFinishedCommandHandler> _mockLogger;
    private MemberHasFinishedCommandHandler _memberHasFinishedCommandHandler;

    [SetUp]
    public void Setup()
    {
        _mockSynchronizationRepository = A.Fake<ISynchronizationRepository>();
        _mockSynchronizationStatusCheckerService = A.Fake<ISynchronizationStatusCheckerService>();
        _mockSynchronizationProgressService = A.Fake<ISynchronizationProgressService>();
        _mockLogger = A.Fake<ILogger<MemberHasFinishedCommandHandler>>();

        _memberHasFinishedCommandHandler = new MemberHasFinishedCommandHandler(
            _mockSynchronizationRepository,
            _mockSynchronizationStatusCheckerService,
            _mockSynchronizationProgressService,
            _mockLogger);
    }

    [Test]
    public async Task Handle_ValidRequest_ProcessesMemberHasFinished()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };

        var request = new MemberHasFinishedRequest(sessionId, client);

        A.CallTo(() => _mockSynchronizationRepository.UpdateIfExists(sessionId, A<Func<SynchronizationEntity, bool>>._, A<ITransaction?>._, A<IRedLock?>._))
            .Invokes((string _, Func<SynchronizationEntity, bool> func, ITransaction? transaction, IRedLock? redisLock) => 
            {
                var synchronization = new SynchronizationEntity
                {
                    Progress = new SynchronizationProgressEntity
                    {
                        Members = new List<string> { "client1", "client2" },
                        CompletedMembers = new List<string> { "client2" }
                    }
                };
                func(synchronization);
            })
            .Returns(new UpdateEntityResult<SynchronizationEntity>(new SynchronizationEntity(), UpdateEntityStatus.Saved));
        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(A<SynchronizationEntity>._, A<bool>._))
            .Returns(Task.CompletedTask);

        // Act
        await _memberHasFinishedCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockSynchronizationRepository.UpdateIfExists(sessionId, A<Func<SynchronizationEntity, bool>>._, A<ITransaction?>._, A<IRedLock?>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(A<SynchronizationEntity>._, A<bool>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };

        var request = new MemberHasFinishedRequest(sessionId, client);
        var expectedException = new InvalidOperationException("Test exception");

        A.CallTo(() => _mockSynchronizationRepository.UpdateIfExists(sessionId, A<Func<SynchronizationEntity, bool>>._, A<ITransaction?>._, A<IRedLock?>._))
            .Throws(expectedException);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => 
            _memberHasFinishedCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().Be(expectedException);
    }
}