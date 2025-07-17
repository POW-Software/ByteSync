using NUnit.Framework;
using Moq;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Services.Communications.Transfers;
using FluentAssertions;

namespace ByteSync.Tests.Services.Communications.Transfers;

public class FilePartDownloadAsserterTests
{
    [Test]
    public async Task AssertAsync_CallsApiClientSuccessfully()
    {
        var apiClient = new Mock<IFileTransferApiClient>();
        var semaphoreSlim = new SemaphoreSlim(1, 1);
        var onErrorCalled = false;
        var asserter = new FilePartDownloadAsserter(apiClient.Object, semaphoreSlim, () => onErrorCalled = true);
        var parameters = new TransferParameters { SharedFileDefinition = new SharedFileDefinition() };
        await asserter.AssertAsync(parameters);
        apiClient.Verify(a => a.AssertFilePartIsDownloaded(parameters), Times.Once);
        onErrorCalled.Should().BeFalse();
    }

    [Test]
    public async Task AssertAsync_OnException_CallsOnError()
    {
        var apiClient = new Mock<IFileTransferApiClient>();
        apiClient.Setup(a => a.AssertFilePartIsDownloaded(It.IsAny<TransferParameters>())).Throws(new Exception("fail"));
        var semaphoreSlim = new SemaphoreSlim(1, 1);
        var onErrorCalled = false;
        var asserter = new FilePartDownloadAsserter(apiClient.Object, semaphoreSlim, () => onErrorCalled = true);
        var parameters = new TransferParameters { SharedFileDefinition = new SharedFileDefinition() };
        await asserter.AssertAsync(parameters);
        onErrorCalled.Should().BeTrue();
    }
} 