using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Services.Communications.Transfers;

namespace ByteSync.Client.Tests.Services.Communications.Transfers;

public class FilePartDownloadAsserterTests
{
    [Test]
    public async Task AssertAsync_CallsApiClientSuccessfully()
    {
        var apiClient = new Mock<IFileTransferApiClient>();
        var syncRoot = new object();
        var onErrorCalled = false;
        var asserter = new FilePartDownloadAsserter(apiClient.Object, syncRoot, () => onErrorCalled = true);
        var parameters = new TransferParameters { SharedFileDefinition = new SharedFileDefinition() };
        await asserter.AssertAsync(parameters);
        apiClient.Verify(a => a.AssertFilePartIsDownloaded(parameters), Times.Once);
        Assert.That(onErrorCalled, Is.False);
    }

    [Test]
    public async Task AssertAsync_OnException_CallsOnError()
    {
        var apiClient = new Mock<IFileTransferApiClient>();
        apiClient.Setup(a => a.AssertFilePartIsDownloaded(It.IsAny<TransferParameters>())).Throws(new Exception("fail"));
        var syncRoot = new object();
        var onErrorCalled = false;
        var asserter = new FilePartDownloadAsserter(apiClient.Object, syncRoot, () => onErrorCalled = true);
        var parameters = new TransferParameters { SharedFileDefinition = new SharedFileDefinition() };
        await asserter.AssertAsync(parameters);
        Assert.That(onErrorCalled, Is.True);
    }
} 