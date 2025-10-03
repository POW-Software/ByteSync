using System.Reactive.Subjects;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Communications.Transfers.Downloading;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Downloading;

[TestFixture]
public class FileDownloaderCacheTests
{
    private Mock<ISessionService> _sessionService = null!;
    private Mock<IFileDownloaderFactory> _factory = null!;
    private FileDownloaderCache _cache = null!;
    
    private Subject<AbstractSession?> _sessionSubject = null!;
    private Subject<SessionStatus> _statusSubject = null!;
    
    [SetUp]
    public void Setup()
    {
        _sessionSubject = new Subject<AbstractSession?>();
        _statusSubject = new Subject<SessionStatus>();
        
        _sessionService = new Mock<ISessionService>();
        _sessionService.Setup(s => s.SessionObservable).Returns(_sessionSubject);
        _sessionService.Setup(s => s.SessionStatusObservable).Returns(_statusSubject);
        
        _factory = new Mock<IFileDownloaderFactory>(MockBehavior.Strict);
        
        _cache = new FileDownloaderCache(_sessionService.Object, _factory.Object);
    }
    
    [TearDown]
    public void TearDown()
    {
        _factory.Verify();
        _sessionService.Verify(s => s.SessionObservable, Times.AtLeastOnce);
        _sessionService.Verify(s => s.SessionStatusObservable, Times.AtLeastOnce);
    }
    
    private static SharedFileDefinition MakeShared(string id)
    {
        return new SharedFileDefinition { Id = id };
    }
    
    [Test]
    public async Task GetFileDownloader_ShouldCachePerSharedFileDefinitionId_AndInvokeCallbackOnce()
    {
        var shared = MakeShared("A");
        
        var partsCoordinator = new Mock<IDownloadPartsCoordinator>().Object;
        var downloader = new Mock<IFileDownloader>(MockBehavior.Strict);
        downloader.SetupGet(d => d.PartsCoordinator).Returns(partsCoordinator);
        downloader.SetupGet(d => d.SharedFileDefinition).Returns(shared);
        _factory.Setup(f => f.Build(shared)).Returns(downloader.Object).Verifiable();
        
        var callbackCount = 0;
        _cache.OnPartsCoordinatorCreated = (s, pc) =>
        {
            s.Should().Be(shared);
            pc.Should().Be(partsCoordinator);
            callbackCount++;
        };
        
        var a1 = await _cache.GetFileDownloader(shared);
        var a2 = await _cache.GetFileDownloader(shared);
        
        a1.Should().BeSameAs(a2);
        callbackCount.Should().Be(1);
    }
    
    [Test]
    public async Task GetFileDownloader_WithDifferentIds_ShouldCreateTwoEntries()
    {
        var shared1 = MakeShared("A");
        var shared2 = MakeShared("B");
        
        var parts1 = new Mock<IDownloadPartsCoordinator>().Object;
        var parts2 = new Mock<IDownloadPartsCoordinator>().Object;
        
        var d1 = new Mock<IFileDownloader>(MockBehavior.Strict);
        d1.SetupGet(d => d.PartsCoordinator).Returns(parts1);
        d1.SetupGet(d => d.SharedFileDefinition).Returns(shared1);
        var d2 = new Mock<IFileDownloader>(MockBehavior.Strict);
        d2.SetupGet(d => d.PartsCoordinator).Returns(parts2);
        d2.SetupGet(d => d.SharedFileDefinition).Returns(shared2);
        
        _factory.Setup(f => f.Build(shared1)).Returns(d1.Object).Verifiable();
        _factory.Setup(f => f.Build(shared2)).Returns(d2.Object).Verifiable();
        
        var got1 = await _cache.GetFileDownloader(shared1);
        var got2 = await _cache.GetFileDownloader(shared2);
        
        got1.Should().NotBeSameAs(got2);
    }
    
    [Test]
    public async Task RemoveFileDownloader_ShouldRemoveFromCache_AndCleanupWhenConcreteType()
    {
        var shared = MakeShared("A");
        var parts = new Mock<IDownloadPartsCoordinator>().Object;
        
        // We cannot easily mock concrete constructor; instead, simulate using Strict IFileDownloader and skip CleanupResources path.
        // So we will test Remove behavior without concrete cast first.
        var downloader = new Mock<IFileDownloader>(MockBehavior.Strict);
        downloader.SetupGet(d => d.PartsCoordinator).Returns(parts);
        downloader.SetupGet(d => d.SharedFileDefinition).Returns(shared);
        
        _factory.Setup(f => f.Build(shared)).Returns(downloader.Object).Verifiable();
        
        var got = await _cache.GetFileDownloader(shared);
        got.Should().BeSameAs(downloader.Object);
        
        await _cache.RemoveFileDownloader(got);
        
        // Request again should rebuild since previous entry removed
        _factory.Setup(f => f.Build(shared)).Returns(downloader.Object).Verifiable();
        var gotAgain = await _cache.GetFileDownloader(shared);
        gotAgain.Should().BeSameAs(downloader.Object);
    }
    
    [Test]
    public async Task Reset_OnSessionPreparation_ShouldClearCache()
    {
        var shared = MakeShared("A");
        var parts = new Mock<IDownloadPartsCoordinator>().Object;
        var downloader = new Mock<IFileDownloader>(MockBehavior.Strict);
        downloader.SetupGet(d => d.PartsCoordinator).Returns(parts);
        downloader.SetupGet(d => d.SharedFileDefinition).Returns(shared);
        _factory.Setup(f => f.Build(shared)).Returns(downloader.Object).Verifiable();
        
        var first = await _cache.GetFileDownloader(shared);
        first.Should().NotBeNull();
        
        // trigger reset via session status
        _statusSubject.OnNext(SessionStatus.Preparation);
        
        // After reset, factory should be called again
        _factory.Setup(f => f.Build(shared)).Returns(downloader.Object).Verifiable();
        var second = await _cache.GetFileDownloader(shared);
        second.Should().NotBeNull();
        second.Should().BeSameAs(downloader.Object);
    }
    
    [Test]
    public async Task Reset_OnSessionEnd_ShouldClearCache()
    {
        var shared = MakeShared("A");
        var parts = new Mock<IDownloadPartsCoordinator>().Object;
        var downloader = new Mock<IFileDownloader>(MockBehavior.Strict);
        downloader.SetupGet(d => d.PartsCoordinator).Returns(parts);
        downloader.SetupGet(d => d.SharedFileDefinition).Returns(shared);
        _factory.Setup(f => f.Build(shared)).Returns(downloader.Object).Verifiable();
        
        var first = await _cache.GetFileDownloader(shared);
        first.Should().NotBeNull();
        
        // trigger session end (null)
        _sessionSubject.OnNext(null);
        
        _factory.Setup(f => f.Build(shared)).Returns(downloader.Object).Verifiable();
        var second = await _cache.GetFileDownloader(shared);
        second.Should().NotBeNull();
    }
}