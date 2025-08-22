using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Services.Communications.Transfers.Downloading;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Communications.Transfers.Downloading;

public class FilePartDownloadAsserterTests
{
    [Test]
    public async Task AssertAsync_CallsApiClientSuccessfully()
    {
        var apiClient = new Mock<IFileTransferApiClient>();
        var semaphoreSlim = new SemaphoreSlim(1, 1);
        var errorManager = new Mock<IErrorManager>().Object;
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<FilePartDownloadAsserter>>().Object;
        var asserter = new FilePartDownloadAsserter(apiClient.Object, semaphoreSlim, errorManager, logger);
        var parameters = new TransferParameters { SharedFileDefinition = new SharedFileDefinition() };
        await asserter.AssertAsync(parameters);
        apiClient.Verify(a => a.AssertFilePartIsDownloaded(parameters), Times.Once);
    }

    [Test]
    public async Task AssertAsync_OnException_CallsOnError()
    {
        var apiClient = new Mock<IFileTransferApiClient>();
        apiClient.Setup(a => a.AssertFilePartIsDownloaded(It.IsAny<TransferParameters>())).Throws(new Exception("fail"));
        var semaphoreSlim = new SemaphoreSlim(1, 1);
        var errorManager = new Mock<IErrorManager>();
        errorManager.Setup(e => e.SetOnErrorAsync()).Returns(Task.CompletedTask).Verifiable();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<FilePartDownloadAsserter>>().Object;
        var asserter = new FilePartDownloadAsserter(apiClient.Object, semaphoreSlim, errorManager.Object, logger);
        var parameters = new TransferParameters { SharedFileDefinition = new SharedFileDefinition() };
        await asserter.AssertAsync(parameters);
        errorManager.Verify(e => e.SetOnErrorAsync(), Times.Once);
    }
} 