using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.FileTransfers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.FileTransfers;

[TestFixture]
public class AssertFilePartIsUploadedCommandHandlerTests
{
    private ICloudSessionsRepository _mockCloudSessionsRepository;
    private ISharedFilesService _mockSharedFilesService;
    private ISynchronizationService _mockSynchronizationService;
    private IInvokeClientsService _mockInvokeClientsService;
    private IUsageStatisticsService _mockUsageStatisticsService;
    private ILogger<AssertFilePartIsUploadedCommandHandler> _mockLogger;
    private AssertFilePartIsUploadedCommandHandler _assertFilePartIsUploadedCommandHandler;
    private ITransferLocationService _mockTransferLocationService = A.Fake<ITransferLocationService>();
    
    [SetUp]
    public void Setup()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockSharedFilesService = A.Fake<ISharedFilesService>();
        _mockSynchronizationService = A.Fake<ISynchronizationService>();
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockUsageStatisticsService = A.Fake<IUsageStatisticsService>();
        _mockLogger = A.Fake<ILogger<AssertFilePartIsUploadedCommandHandler>>();
        _mockTransferLocationService = A.Fake<ITransferLocationService>();
        
        _assertFilePartIsUploadedCommandHandler = new AssertFilePartIsUploadedCommandHandler(
            _mockCloudSessionsRepository,
            _mockSharedFilesService,
            _mockSynchronizationService,
            _mockInvokeClientsService,
            _mockUsageStatisticsService,
            _mockLogger,
            _mockTransferLocationService);
    }

    [Test]
    public async Task Handle_ValidRequest_AssertsFilePartIsUploaded()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = new SharedFileDefinition { Id = "file1" },
            PartNumber = 1
        };

        var request = new AssertFilePartIsUploadedRequest(sessionId, client, transferParameters);
        
        // Mock the session repository to return a valid session member
        var mockSessionMember = new SessionMemberData { ClientInstanceId = client.ClientInstanceId };
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client)).Returns(mockSessionMember);
        
        // Mock the transfer location service to return true for IsSharedFileDefinitionAllowed
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, transferParameters.SharedFileDefinition))
            .Returns(true);

        // Mock the usage statistics service
        A.CallTo(() => _mockUsageStatisticsService.RegisterUploadUsage(client, transferParameters.SharedFileDefinition, transferParameters.PartNumber!.Value))
            .Returns(Task.CompletedTask);

        // Act
        await _assertFilePartIsUploadedCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockUsageStatisticsService.RegisterUploadUsage(client, transferParameters.SharedFileDefinition, transferParameters.PartNumber!.Value))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WhenServiceThrowsException_PropagatesException()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = new SharedFileDefinition { Id = "file1" },
            PartNumber = 1
        };
        var expectedException = new InvalidOperationException("Test exception");

        var request = new AssertFilePartIsUploadedRequest(sessionId, client, transferParameters);

        // Mock the session repository to throw an exception
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Throws(expectedException);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => 
            _assertFilePartIsUploadedCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().Be(expectedException);
    }
} 