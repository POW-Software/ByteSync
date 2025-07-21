using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Factories;
using ByteSync.Services.Communications.Transfers;
using ByteSync.Factories;
using ByteSync.Interfaces;
using FluentAssertions;

namespace ByteSync.Tests.Services.Communications.Transfers;

public class FileDownloaderFactoryTests
{
    [Test]
    public void Build_CreatesFileDownloaderWithAllDependencies()
    {
        var policyFactory = new Mock<IPolicyFactory>();
        var downloadTargetBuilder = new Mock<IDownloadTargetBuilder>();
        var fileTransferApiClient = new Mock<IFileTransferApiClient>();
        var mergerDecrypterFactory = new Mock<IMergerDecrypterFactory>();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<ByteSync.Services.Communications.Transfers.FilePartDownloadAsserter>>();
        var fileDownloaderLogger = new Mock<Microsoft.Extensions.Logging.ILogger<ByteSync.Services.Communications.Transfers.FileDownloader>>();
        var sharedFileDefinition = new SharedFileDefinition();
        var downloadTarget = new ByteSync.Business.Communications.Downloading.DownloadTarget(sharedFileDefinition, null, new HashSet<string> { "file1" });
        downloadTargetBuilder.Setup(b => b.BuildDownloadTarget(sharedFileDefinition)).Returns(downloadTarget);
        mergerDecrypterFactory.Setup(f => f.Build(It.IsAny<string>(), downloadTarget, It.IsAny<System.Threading.CancellationTokenSource>()))
            .Returns(new Mock<ByteSync.Interfaces.Controls.Encryptions.IMergerDecrypter>().Object);
        var factory = new FileDownloaderFactory(policyFactory.Object, downloadTargetBuilder.Object, fileTransferApiClient.Object, mergerDecrypterFactory.Object, logger.Object, fileDownloaderLogger.Object);
        var downloader = factory.Build(sharedFileDefinition);
        downloader.Should().NotBeNull();
        downloader.SharedFileDefinition.Should().BeSameAs(sharedFileDefinition);
        downloader.DownloadTarget.Should().BeSameAs(downloadTarget);
    }
} 