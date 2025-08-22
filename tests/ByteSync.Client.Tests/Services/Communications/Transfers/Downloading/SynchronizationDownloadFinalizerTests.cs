using System.IO;
using System.IO.Compression;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Factories;
using ByteSync.Services.Communications.Transfers.Downloading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Communications.Transfers.Downloading;

[TestFixture]
public class SynchronizationDownloadFinalizerTests
{
    private Mock<IDeltaManager> _deltaManager = null!;
    private Mock<ITemporaryFileManagerFactory> _temporaryFileManagerFactory = null!;
    private Mock<IFileDatesSetter> _fileDatesSetter = null!;
    private Mock<ILogger<SynchronizationDownloadFinalizer>> _logger = null!;

    private SynchronizationDownloadFinalizer _sut = null!;

    [SetUp]
    public void Setup()
    {
        _deltaManager = new Mock<IDeltaManager>(MockBehavior.Strict);
        _temporaryFileManagerFactory = new Mock<ITemporaryFileManagerFactory>(MockBehavior.Strict);
        _fileDatesSetter = new Mock<IFileDatesSetter>(MockBehavior.Strict);
        _logger = new Mock<ILogger<SynchronizationDownloadFinalizer>>();

        _sut = new SynchronizationDownloadFinalizer(
            _deltaManager.Object,
            _temporaryFileManagerFactory.Object,
            _fileDatesSetter.Object,
            _logger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        // Verify all expected (Verifiable) setups were called
        _deltaManager.Verify();
        _temporaryFileManagerFactory.Verify();
        _fileDatesSetter.Verify();
    }

    [Test]
    public async Task FinalizeSynchronization_MonoFile_Delta_AppliesDeltaForEachDestination_AndDeletesTemp()
    {
        var shared = new SharedFileDefinition
        {
            Id = "sf-id",
            SharedFileType = SharedFileTypes.DeltaSynchronization,
            ActionsGroupIds = ["ag1"]
        };

        var downloadTarget = new DownloadTarget(shared, null, ["temp.delta"])
        {
            FinalDestinationsPerActionsGroupId = new Dictionary<string, HashSet<string>>
            {
                ["ag1"] = [GetTempFilePath(), GetTempFilePath()]
            }
        };

        foreach (var dest in downloadTarget.FinalDestinationsPerActionsGroupId["ag1"])
        {
            _deltaManager.Setup(m => m.ApplyDelta(dest, "temp.delta")).Returns(Task.CompletedTask).Verifiable();
            _fileDatesSetter.Setup(f => f.SetDates(shared, dest, null)).Returns(Task.CompletedTask).Verifiable();
        }

        await _sut.FinalizeSynchronization(shared, downloadTarget);

        foreach (var dest in downloadTarget.FinalDestinationsPerActionsGroupId["ag1"])
        {
            _deltaManager.Verify(m => m.ApplyDelta(dest, "temp.delta"), Times.Once);
            _fileDatesSetter.Verify(f => f.SetDates(shared, dest, null), Times.Once);
        }

        // temp file should be deleted
        File.Exists("temp.delta").Should().BeFalse();
    }

    [Test]
    public async Task FinalizeSynchronization_MonoFile_NonDelta_SetsDatesForAllFinalDestinations()
    {
        var shared = new SharedFileDefinition
        {
            Id = "sf-id",
            SharedFileType = SharedFileTypes.FullSynchronization,
            ActionsGroupIds = ["ag1"]
        };

        var downloadTarget = new DownloadTarget(shared, null, [])
        {
            FinalDestinationsPerActionsGroupId = new Dictionary<string, HashSet<string>>
            {
                ["ag1"] = [GetTempFilePath(), GetTempFilePath(), GetTempFilePath()]
            }
        };

        foreach (var dest in downloadTarget.AllFinalDestinations)
        {
            _fileDatesSetter.Setup(f => f.SetDates(shared, dest, null)).Returns(Task.CompletedTask).Verifiable();
        }

        await _sut.FinalizeSynchronization(shared, downloadTarget);

        foreach (var dest in downloadTarget.AllFinalDestinations)
        {
            _fileDatesSetter.Verify(f => f.SetDates(shared, dest, null), Times.Once);
        }
    }

    [Test]
    public async Task FinalizeSynchronization_MultiFileZip_Delta_ExtractsStreamsAndAppliesDeltaAndSetsDates()
    {
        var shared = new SharedFileDefinition
        {
            Id = "sf-id",
            SharedFileType = SharedFileTypes.DeltaSynchronization,
            IsMultiFileZip = true
        };

        var zipPath = GetNewTempPath(".zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            CreateEntryWithContent(archive, "ag1", "delta-content-1");
            CreateEntryWithContent(archive, "ag2", "delta-content-2");
        }

        var finalMap = new Dictionary<string, HashSet<string>>
        {
            ["ag1"] = [GetTempFilePath()],
            ["ag2"] = [GetTempFilePath(), GetTempFilePath()]
        };

        var downloadTarget = new DownloadTarget(shared, null, [zipPath])
        {
            FinalDestinationsPerActionsGroupId = finalMap,
            IsMultiFileZip = true
        };

        foreach (var kvp in finalMap)
        {
            foreach (var dest in kvp.Value)
            {
                _deltaManager.Setup(m => m.ApplyDelta(dest, It.IsAny<Stream>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();
                _fileDatesSetter.Setup(f => f.SetDates(shared, dest, null)).Returns(Task.CompletedTask).Verifiable();
            }
        }

        await _sut.FinalizeSynchronization(shared, downloadTarget);

        foreach (var kvp in finalMap)
        {
            foreach (var dest in kvp.Value)
            {
                _deltaManager.Verify(m => m.ApplyDelta(dest, It.IsAny<Stream>()), Times.Once);
                _fileDatesSetter.Verify(f => f.SetDates(shared, dest, null), Times.Once);
            }
        }

        File.Exists(zipPath).Should().BeFalse();
    }

    [Test]
    public async Task FinalizeSynchronization_MultiFileZip_NonDelta_ExtractsEachEntry_Validates_TryRevertOnErrorOnException_AndSetsDates()
    {
        var shared = new SharedFileDefinition
        {
            Id = "sf-id",
            SharedFileType = SharedFileTypes.FullSynchronization,
            IsMultiFileZip = true
        };

        var zipPath = GetNewTempPath(".zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            CreateEntryWithContent(archive, "ag1", "content-1");
            CreateEntryWithContent(archive, "ag2", "content-2");
        }

        var dest1 = GetTempFilePath();
        var dest2 = GetTempFilePath();
        var finalMap = new Dictionary<string, HashSet<string>>
        {
            ["ag1"] = [dest1],
            ["ag2"] = [dest2]
        };

        var tempManagers = new Dictionary<string, Mock<ITemporaryFileManager>>();
        _temporaryFileManagerFactory
            .Setup(f => f.Create(It.IsAny<string>()))
            .Returns<string>(finalDestination =>
            {
                var mock = new Mock<ITemporaryFileManager>(MockBehavior.Strict);
                mock.Setup(m => m.GetDestinationTemporaryPath()).Returns(finalDestination + ".tmp");
                mock.Setup(m => m.ValidateTemporaryFile());
                mock.Setup(m => m.TryRevertOnError(It.IsAny<Exception>()));
                tempManagers[finalDestination] = mock;
                return mock.Object;
            })
            .Verifiable();

        var downloadTarget = new DownloadTarget(shared, null, [zipPath])
        {
            FinalDestinationsPerActionsGroupId = finalMap,
            IsMultiFileZip = true
        };

        _fileDatesSetter.Setup(f => f.SetDates(shared, dest1, null)).Returns(Task.CompletedTask).Verifiable();
        _fileDatesSetter.Setup(f => f.SetDates(shared, dest2, null)).Returns(Task.CompletedTask).Verifiable();

        await _sut.FinalizeSynchronization(shared, downloadTarget);

        _temporaryFileManagerFactory.Verify(f => f.Create(dest1), Times.Once);
        _temporaryFileManagerFactory.Verify(f => f.Create(dest2), Times.Once);
        tempManagers[dest1].Verify(m => m.ValidateTemporaryFile(), Times.Once);
        tempManagers[dest2].Verify(m => m.ValidateTemporaryFile(), Times.Once);

        // Also verify file dates were set
        _fileDatesSetter.Verify(f => f.SetDates(shared, dest1, null), Times.Once);
        _fileDatesSetter.Verify(f => f.SetDates(shared, dest2, null), Times.Once);

        File.Exists(zipPath).Should().BeFalse();
    }

    [Test]
    public async Task FinalizeSynchronization_MultiFileZip_NonDelta_WhenExtractThrows_CallsTryRevertOnErrorAndRethrows()
    {
        var shared = new SharedFileDefinition
        {
            Id = "sf-id",
            SharedFileType = SharedFileTypes.FullSynchronization,
            IsMultiFileZip = true
        };

        var zipPath = GetNewTempPath(".zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            CreateEntryWithContent(archive, "ag1", "content-1");
        }

        var dest = GetTempFilePath();
        var finalMap = new Dictionary<string, HashSet<string>>
        {
            ["ag1"] = [dest]
        };

        var tempManager = new Mock<ITemporaryFileManager>(MockBehavior.Strict);
        tempManager.Setup(m => m.GetDestinationTemporaryPath()).Returns(dest + ".tmp");
        tempManager.Setup(m => m.ValidateTemporaryFile()).Throws(new IOException("validation failed"));
        tempManager.Setup(m => m.TryRevertOnError(It.IsAny<Exception>())).Verifiable();
        _temporaryFileManagerFactory.Setup(f => f.Create(dest)).Returns(tempManager.Object).Verifiable();

        var downloadTarget = new DownloadTarget(shared, null, [zipPath])
        {
            FinalDestinationsPerActionsGroupId = finalMap,
            IsMultiFileZip = true
        };

        var act = async () => await _sut.FinalizeSynchronization(shared, downloadTarget);
        await act.Should().ThrowAsync<IOException>();
        tempManager.Verify(m => m.TryRevertOnError(It.IsAny<Exception>()), Times.Once);
    }

    private static void CreateEntryWithContent(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        writer.Write(content);
    }

    private static string GetTempFilePath()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        return path;
    }

    private static string GetNewTempPath(string extension)
    {
        return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);
    }
}
