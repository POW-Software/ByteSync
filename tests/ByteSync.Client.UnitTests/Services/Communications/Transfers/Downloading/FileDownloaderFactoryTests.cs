using Autofac;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Factories;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Factories;
using ByteSync.TestsCommon.Mocking;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Downloading;

public class FileDownloaderFactoryTests
{
    [Test]
    public void Build_CreatesFileDownloaderWithAllDependencies()
    {
        // Arrange
        var builder = new ContainerBuilder();
        builder.RegisterSource(new MoqRegistrationSource());
        
        var sharedFileDefinition = new SharedFileDefinition();
        var downloadTarget = new DownloadTarget(sharedFileDefinition, null, new HashSet<string> { "file1" });
        
        var mockFileDownloader = new Mock<IFileDownloader>();
        mockFileDownloader.Setup(f => f.SharedFileDefinition).Returns(sharedFileDefinition);
        mockFileDownloader.Setup(f => f.DownloadTarget).Returns(downloadTarget);
        
        builder.RegisterInstance(mockFileDownloader.Object).As<IFileDownloader>();
        
        var container = builder.Build();
        var context = container.Resolve<IComponentContext>();
        
        var downloadTargetBuilder = container.Resolve<Mock<IDownloadTargetBuilder>>();
        var mergerDecrypterFactory = container.Resolve<Mock<IMergerDecrypterFactory>>();
        
        downloadTargetBuilder.Setup(b => b.BuildDownloadTarget(sharedFileDefinition)).Returns(downloadTarget);
        mergerDecrypterFactory.Setup(f => f.Build(It.IsAny<string>(), downloadTarget, It.IsAny<CancellationTokenSource>()))
            .Returns(new Mock<IMergerDecrypter>().Object);
        
        // Act
        var factory = new FileDownloaderFactory(context);
        var downloader = factory.Build(sharedFileDefinition);
        
        // Assert
        downloader.Should().NotBeNull();
        downloader.SharedFileDefinition.Should().BeSameAs(sharedFileDefinition);
        downloader.DownloadTarget.Should().BeSameAs(downloadTarget);
    }
}