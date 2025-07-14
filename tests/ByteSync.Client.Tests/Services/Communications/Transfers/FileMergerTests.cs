using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Services.Communications.Transfers;

namespace ByteSync.Client.Tests.Services.Communications.Transfers;

public class FileMergerTests
{
    [Test]
    public async Task MergeAsync_CallsAllDecryptersAndRemovesMemoryStreamAndNotifies()
    {
        var decrypter1 = new Mock<IMergerDecrypter>();
        var decrypter2 = new Mock<IMergerDecrypter>();
        var merged = false;
        var removed = false;
        var fileMerger = new FileMerger(
            new List<IMergerDecrypter> { decrypter1.Object, decrypter2.Object },
            part => merged = true,
            () => { },
            part => removed = true,
            new object()
        );
        await fileMerger.MergeAsync(42);
        decrypter1.Verify(d => d.MergeAndDecrypt(), Times.Once);
        decrypter2.Verify(d => d.MergeAndDecrypt(), Times.Once);
        Assert.That(merged, Is.True);
        Assert.That(removed, Is.True);
    }

    [Test]
    public async Task MergeAsync_OnError_CallsOnErrorAndThrows()
    {
        var decrypter = new Mock<IMergerDecrypter>();
        decrypter.Setup(d => d.MergeAndDecrypt()).ThrowsAsync(new Exception("fail"));
        var errorCalled = false;
        var fileMerger = new FileMerger(
            new List<IMergerDecrypter> { decrypter.Object },
            part => { },
            () => errorCalled = true,
            part => { },
            new object()
        );
        Assert.That(async () => await fileMerger.MergeAsync(1), Throws.TypeOf<Exception>());
        Assert.That(errorCalled, Is.True);
    }
    
} 