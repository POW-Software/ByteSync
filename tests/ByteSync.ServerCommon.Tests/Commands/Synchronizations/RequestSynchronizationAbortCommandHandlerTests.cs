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
public class RequestSynchronizationAbortCommandHandlerTests
{
    private ISynchronizationRepository _mockSynchronizationRepository;
    private ISynchronizationStatusCheckerService _mockSynchronizationStatusCheckerService;
    private ISynchronizationProgressService _mockSynchronizationProgressService;
    private ILogger<RequestSynchronizationAbortCommandHandler> _mockLogger;
    private RequestSynchronizationAbortCommandHandler _requestSynchronizationAbortCommandHandler;

    [SetUp]
    public void Setup()
    {
        _mockSynchronizationRepository = A.Fake<ISynchronizationRepository>();
        _mockSynchronizationStatusCheckerService = A.Fake<ISynchronizationStatusCheckerService>();
        _mockSynchronizationProgressService = A.Fake<ISynchronizationProgressService>();
        _mockLogger = A.Fake<ILogger<RequestSynchronizationAbortCommandHandler>>();

        _requestSynchronizationAbortCommandHandler = new RequestSynchronizationAbortCommandHandler(
            _mockSynchronizationRepository,
            _mockSynchronizationStatusCheckerService,
            _mockSynchronizationProgressService,
            _mockLogger);
    }

    [Test]
    public async Task Handle_ValidRequest_RequestsSynchronizationAbort()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };

        var request = new RequestSynchronizationAbortRequest(sessionId, client);

        A.CallTo(() => _mockSynchronizationStatusCheckerService.CheckSynchronizationCanBeAborted(A<SynchronizationEntity>._))
            .Returns(true);
        A.CallTo(() => _mockSynchronizationRepository.UpdateIfExists(sessionId, A<Func<SynchronizationEntity, bool>>._, A<ITransaction?>._, A<IRedLock?>._))
            .Invokes((string _, Func<SynchronizationEntity, bool> func, ITransaction? _, IRedLock? _) => 
            {
                var synchronization = new SynchronizationEntity
                {
                    AbortRequestedBy = new List<string>()
                };
                func(synchronization);
            })
            .Returns(new UpdateEntityResult<SynchronizationEntity>(new SynchronizationEntity(), UpdateEntityStatus.Saved));
        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(A<SynchronizationEntity>._, A<bool>._))
            .Returns(Task.CompletedTask);

        // Act
        await _requestSynchronizationAbortCommandHandler.Handle(request, CancellationToken.None);

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

        var request = new RequestSynchronizationAbortRequest(sessionId, client);
        var expectedException = new InvalidOperationException("Test exception");

        A.CallTo(() => _mockSynchronizationRepository.UpdateIfExists(sessionId, A<Func<SynchronizationEntity, bool>>._, A<ITransaction?>._, A<IRedLock?>._))
            .Throws(expectedException);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => 
            _requestSynchronizationAbortCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().Be(expectedException);
    }
}