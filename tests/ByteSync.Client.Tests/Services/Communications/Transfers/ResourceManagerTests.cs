using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Services.Communications.Transfers;

namespace ByteSync.Client.Tests.Services.Communications.Transfers;

public class ResourceManagerTests
{
    [Test]
    public void Cleanup_ClearsDownloadPartsInfoAndMemoryStreams()
    {
        var downloadPartsInfo = new DownloadPartsInfo();
        downloadPartsInfo.AvailableParts.Add(1);
        downloadPartsInfo.DownloadedParts.Add(2);
        var downloadTarget = new DownloadTarget(null!, null, new HashSet<string>());
        downloadTarget.GetMemoryStream(1);
        downloadTarget.GetMemoryStream(2);
        var manager = new ResourceManager(downloadPartsInfo, downloadTarget);
        manager.Cleanup();
        Assert.That(downloadPartsInfo.AvailableParts, Is.Empty);
        Assert.That(downloadPartsInfo.DownloadedParts, Is.Empty);
        Assert.That(downloadTarget.MemoryStreams, Is.Empty);
    }
} 