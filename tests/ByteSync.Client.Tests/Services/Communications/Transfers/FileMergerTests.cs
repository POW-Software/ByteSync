using ByteSync.Interfaces.Controls.Communications;
using NUnit.Framework;
using Moq;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Services.Communications.Transfers;
using FluentAssertions;
using System.Security.Cryptography;

namespace ByteSync.Tests.Services.Communications.Transfers;

public class FileMergerTests
{
    [Test]
    public async Task MergeAsync_CallsAllDecryptersAndRemovesMemoryStreamAndNotifies()
    {
        var decrypter1 = new Mock<IMergerDecrypter>();
        var decrypter2 = new Mock<IMergerDecrypter>();
        var errorManager = new Mock<IErrorManager>().Object;
        var downloadTarget = new ByteSync.Business.Communications.Downloading.DownloadTarget(null!, null, new HashSet<string>());
        var fileMerger = new FileMerger(
            new List<IMergerDecrypter> { decrypter1.Object, decrypter2.Object },
            errorManager,
            downloadTarget,
            new SemaphoreSlim(1, 1)
        );
        await fileMerger.MergeAsync(42);
        decrypter1.Verify(d => d.MergeAndDecrypt(), Times.Once);
        decrypter2.Verify(d => d.MergeAndDecrypt(), Times.Once);
        decrypter1.Verify(d => d.Dispose(), Times.Once);
        decrypter2.Verify(d => d.Dispose(), Times.Once);
    }

    [Test]
    public async Task MergeAsync_OnError_CallsOnErrorAndThrows()
    {
        var decrypter = new Mock<IMergerDecrypter>();
        decrypter.Setup(d => d.MergeAndDecrypt()).ThrowsAsync(new Exception("fail"));
        var errorManager = new Mock<IErrorManager>();
        errorManager.Setup(e => e.SetOnErrorAsync()).Returns(Task.CompletedTask).Verifiable();
        var downloadTarget = new ByteSync.Business.Communications.Downloading.DownloadTarget(null!, null, new HashSet<string>());
        var fileMerger = new FileMerger(
            new List<IMergerDecrypter> { decrypter.Object },
            errorManager.Object,
            downloadTarget,
            new SemaphoreSlim(1, 1)
        );
        await FluentActions.Invoking(async () => await fileMerger.MergeAsync(1)).Should().ThrowAsync<InvalidOperationException>();
        errorManager.Verify(e => e.SetOnErrorAsync(), Times.Once);
        decrypter.Verify(d => d.Dispose(), Times.Once);
    }

    [Test]
    public async Task MergeAsync_OnCryptographicError_CallsOnErrorAndThrowsSecureException()
    {
        var decrypter = new Mock<IMergerDecrypter>();
        decrypter.Setup(d => d.MergeAndDecrypt()).ThrowsAsync(new CryptographicException("crypto fail"));
        var errorManager = new Mock<IErrorManager>();
        errorManager.Setup(e => e.SetOnErrorAsync()).Returns(Task.CompletedTask).Verifiable();
        var downloadTarget = new ByteSync.Business.Communications.Downloading.DownloadTarget(null!, null, new HashSet<string>());
        var fileMerger = new FileMerger(
            new List<IMergerDecrypter> { decrypter.Object },
            errorManager.Object,
            downloadTarget,
            new SemaphoreSlim(1, 1)
        );
        var exception = await FluentActions.Invoking(async () => await fileMerger.MergeAsync(1)).Should().ThrowAsync<InvalidOperationException>();
        exception.WithMessage("Cryptographic operation failed");
        errorManager.Verify(e => e.SetOnErrorAsync(), Times.Once);
        decrypter.Verify(d => d.Dispose(), Times.Once);
    }
    
} 