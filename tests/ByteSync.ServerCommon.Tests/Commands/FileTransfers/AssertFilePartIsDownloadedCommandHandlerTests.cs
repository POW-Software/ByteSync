using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.FileTransfers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using ByteSync.ServerCommon.Entities;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Commands.FileTransfers;

[TestFixture]
public class AssertFilePartIsDownloadedCommandHandlerTests
{
    private ICloudSessionsRepository _mockCloudSessionsRepository;
    private ISharedFilesService _mockSharedFilesService;
    private ISynchronizationRepository _mockSynchronizationRepository;
    private ISynchronizationStatusCheckerService _mockSynchronizationStatusCheckerService;
    private ILogger<AssertFilePartIsDownloadedCommandHandler> _mockLogger;
    private AssertFilePartIsDownloadedCommandHandler _assertFilePartIsDownloadedCommandHandler;
    private ITransferLocationService _mockTransferLocationService;
    
    [SetUp]
    public void Setup()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockSharedFilesService = A.Fake<ISharedFilesService>();
        _mockSynchronizationRepository = A.Fake<ISynchronizationRepository>();
        _mockSynchronizationStatusCheckerService = A.Fake<ISynchronizationStatusCheckerService>();
        _mockLogger = A.Fake<ILogger<AssertFilePartIsDownloadedCommandHandler>>();
        _mockTransferLocationService = A.Fake<ITransferLocationService>();
        
        _assertFilePartIsDownloadedCommandHandler = new AssertFilePartIsDownloadedCommandHandler(
            _mockCloudSessionsRepository,
            _mockSharedFilesService,
            _mockSynchronizationRepository,
            _mockSynchronizationStatusCheckerService,
            _mockTransferLocationService,
            _mockLogger);
    }

    [Test]
    [TestCase(StorageProvider.AzureBlobStorage)]
    [TestCase(StorageProvider.CloudflareR2)]
    public async Task Handle_ValidRequest_AssertsFilePartIsDownloaded(StorageProvider storageProvider)
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition { Id = "file1" };
        var partNumber = 1;

        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = sharedFileDefinition,
            PartNumber = partNumber,
            StorageProvider = storageProvider
        };
        var request = new AssertFilePartIsDownloadedRequest(sessionId, client, transferParameters);

        // Mock the session repository to return a valid session member
        var mockSessionMember = new SessionMemberData { ClientInstanceId = client.ClientInstanceId };
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client)).Returns(mockSessionMember);
        
        // Mock the transfer location service to return true for IsSharedFileDefinitionAllowed
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, sharedFileDefinition))
            .Returns(true);

        // Mock the shared files service
        A.CallTo(() => _mockSharedFilesService.AssertFilePartIsDownloaded(client, transferParameters))
            .Returns(Task.CompletedTask);

        // Act
        await _assertFilePartIsDownloadedCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, sharedFileDefinition))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSharedFilesService.AssertFilePartIsDownloaded(client, transferParameters))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    [TestCase(StorageProvider.AzureBlobStorage)]
    [TestCase(StorageProvider.CloudflareR2)]
    public async Task Handle_WhenServiceThrowsException_PropagatesException(StorageProvider storageProvider)
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition { Id = "file1" };
        var partNumber = 1;
        var expectedException = new InvalidOperationException("Test exception");

        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = sharedFileDefinition,
            PartNumber = partNumber,
            StorageProvider = storageProvider
        };
        var request = new AssertFilePartIsDownloadedRequest(sessionId, client, transferParameters);

        // Mock the session repository to throw an exception
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client))
            .Throws(expectedException);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => 
            _assertFilePartIsDownloadedCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().Be(expectedException);
    }

    [Test]
    public async Task Handle_WithPartSizeAndSynchronization_UpdatesDownloadedVolume()
    {
        // Arrange
        const string sessionId = "session-sync";
        var client = new Client { ClientInstanceId = "client-sync" };
        var sharedFileDefinition = new SharedFileDefinition
        {
            Id = "file-sync",
            SessionId = sessionId,
            SharedFileType = SharedFileTypes.FullSynchronization
        }; // IsSynchronization == true

        const long partSize = 1234;
        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = sharedFileDefinition,
            PartNumber = 2,
            PartSizeInBytes = partSize,
            StorageProvider = StorageProvider.AzureBlobStorage
        };
        var request = new AssertFilePartIsDownloadedRequest(sessionId, client, transferParameters);

        var mockSessionMember = new SessionMemberData { ClientInstanceId = client.ClientInstanceId };
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client)).Returns(mockSessionMember);
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, sharedFileDefinition))
            .Returns(true);

        A.CallTo(() => _mockSharedFilesService.AssertFilePartIsDownloaded(client, transferParameters))
            .Returns(Task.CompletedTask);

        // Ensure status checker allows update and validate the increment happens
        A.CallTo(() => _mockSynchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(A<SynchronizationEntity>._))
            .Returns(true);

        A.CallTo(() => _mockSynchronizationRepository.UpdateIfExists(
                sessionId,
                A<Func<SynchronizationEntity, bool>>._,
                A<ITransaction?>._,
                A<IRedLock?>._))
            .Invokes(call =>
            {
                var updater = call.GetArgument<Func<SynchronizationEntity, bool>>(1);
                var sync = new SynchronizationEntity { SessionId = sessionId };
                var before = sync.Progress.ActualDownloadedVolume;
                var result = updater(sync);

                result.Should().BeTrue();
                sync.Progress.ActualDownloadedVolume.Should().Be(before + partSize);
            })
            .Returns(Task.FromResult(new ByteSync.ServerCommon.Business.Repositories.UpdateEntityResult<SynchronizationEntity>(
                null, ByteSync.ServerCommon.Business.Repositories.UpdateEntityStatus.Saved)));

        // Act
        await _assertFilePartIsDownloadedCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockSynchronizationRepository.UpdateIfExists(
                sessionId,
                A<Func<SynchronizationEntity, bool>>._,
                A<ITransaction?>._,
                A<IRedLock?>._))
            .MustHaveHappenedOnceExactly();
    }
    
} 
