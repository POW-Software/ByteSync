using System;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.CloudSessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.ServerCommon.Tests.Commands.CloudSessions;

[TestFixture]
public class ResetSessionCommandHandlerTests
{
    [Test]
    public async Task Handle_CallsAllRequiredServices()
    {
        // Arrange
        var mockCloudSessionsRepository = new Mock<ICloudSessionsRepository>();
        var mockInventoryService = new Mock<IInventoryService>();
        var mockSynchronizationService = new Mock<ISynchronizationService>();
        var mockSharedFilesService = new Mock<ISharedFilesService>();
        var mockInvokeClientsService = new Mock<IInvokeClientsService>();
        var mockLogger = new Mock<ILogger<ResetSessionCommandHandler>>();
        
        var client = new Client();
        var sessionId = "session1";
        
        mockCloudSessionsRepository.Setup(r => r.Update(sessionId, It.IsAny<Func<CloudSessionData, bool>>(), null, null))
            .ReturnsAsync(new UpdateEntityResult<CloudSessionData>(null, UpdateEntityStatus.Saved));
        mockInventoryService.Setup(s => s.ResetSession(sessionId)).Returns(Task.CompletedTask);
        mockSynchronizationService.Setup(s => s.ResetSession(sessionId)).Returns(Task.CompletedTask);
        mockSharedFilesService.Setup(s => s.ClearSession(sessionId)).Returns(Task.CompletedTask);
        mockInvokeClientsService.Setup(s => s.SessionGroupExcept(sessionId, client))
            .Returns(Mock.Of<IHubByteSyncPush>());
        
        var handler = new ResetSessionCommandHandler(
            mockCloudSessionsRepository.Object,
            mockInventoryService.Object,
            mockSynchronizationService.Object,
            mockSharedFilesService.Object,
            mockInvokeClientsService.Object,
            mockLogger.Object);
        
        var request = new ResetSessionRequest(sessionId, client);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        mockCloudSessionsRepository.Verify(r => r.Update(sessionId, It.IsAny<Func<CloudSessionData, bool>>(), null, null), Times.Once);
        mockInventoryService.Verify(s => s.ResetSession(sessionId), Times.Once);
        mockSynchronizationService.Verify(s => s.ResetSession(sessionId), Times.Once);
        mockSharedFilesService.Verify(s => s.ClearSession(sessionId), Times.Once);
        mockInvokeClientsService.Verify(s => s.SessionGroupExcept(sessionId, client), Times.Once);
    }
} 