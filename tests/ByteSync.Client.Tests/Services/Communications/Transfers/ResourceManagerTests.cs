using NUnit.Framework;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Services.Communications.Transfers;
using FluentAssertions;

namespace ByteSync.Tests.Services.Communications.Transfers;

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
        downloadPartsInfo.AvailableParts.Should().BeEmpty();
        downloadPartsInfo.DownloadedParts.Should().BeEmpty();
        downloadTarget.MemoryStreams.Should().BeEmpty();
    }
} 