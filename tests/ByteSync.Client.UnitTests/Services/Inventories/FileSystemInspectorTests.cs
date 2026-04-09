using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Services.Inventories;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Inventories;

public class FileSystemInspectorTests : AbstractTester
{
    [SetUp]
    public void SetUp()
    {
        CreateTestDirectory();
    }

    [TearDown]
    public void TearDown()
    {
        if (TestDirectory?.Exists == true)
        {
            TestDirectory.Delete(true);
        }
    }

    [Test]
    public void ClassifyEntry_ReturnsDirectory_ForDirectoryInfo()
    {
        var posix = new Mock<IPosixFileTypeClassifier>(MockBehavior.Strict);
        posix.Setup(p => p.ClassifyPosixEntry(It.IsAny<string>())).Returns(FileSystemEntryKind.Unknown);
        var inspector = new FileSystemInspector(posix.Object);
        var tempDirectory = Directory.CreateDirectory(Path.Combine(TestDirectory.FullName, Guid.NewGuid().ToString("N")));

        var result = inspector.ClassifyEntry(tempDirectory);

        result.Should().Be(FileSystemEntryKind.Directory);
    }

    [Test]
    public void ClassifyEntry_ReturnsRegularFile_ForFileInfo()
    {
        var posix = new Mock<IPosixFileTypeClassifier>(MockBehavior.Strict);
        posix.Setup(p => p.ClassifyPosixEntry(It.IsAny<string>())).Returns(FileSystemEntryKind.Unknown);
        var inspector = new FileSystemInspector(posix.Object);
        var tempDirectory = Directory.CreateDirectory(Path.Combine(TestDirectory.FullName, Guid.NewGuid().ToString("N")));
        var tempFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
        File.WriteAllText(tempFilePath, "x");
        var fileInfo = new FileInfo(tempFilePath);

        var result = inspector.ClassifyEntry(fileInfo);

        result.Should().Be(FileSystemEntryKind.RegularFile);
    }

    [Test]
    public void ClassifyEntry_ReturnsSymlink_WhenLinkTargetExists()
    {
        var posix = new Mock<IPosixFileTypeClassifier>(MockBehavior.Strict);
        posix.Setup(p => p.ClassifyPosixEntry(It.IsAny<string>())).Returns(FileSystemEntryKind.Unknown);
        var inspector = new FileSystemInspector(posix.Object);
        var tempDirectory = Directory.CreateDirectory(Path.Combine(TestDirectory.FullName, Guid.NewGuid().ToString("N")));
        var targetPath = Path.Combine(tempDirectory.FullName, "target.txt");
        File.WriteAllText(targetPath, "x");
        var linkPath = Path.Combine(tempDirectory.FullName, "link.txt");

        try
        {
            File.CreateSymbolicLink(linkPath, targetPath);
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Symbolic link creation failed: {ex.GetType().Name}");
        }

        var result = inspector.ClassifyEntry(new FileInfo(linkPath));

        result.Should().Be(FileSystemEntryKind.Symlink);
    }

    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public void ClassifyEntry_ReturnsPosixSpecialKind_WhenClassifierProvidesOne()
    {
        var posix = new Mock<IPosixFileTypeClassifier>(MockBehavior.Strict);
        posix.Setup(p => p.ClassifyPosixEntry(It.IsAny<string>())).Returns(FileSystemEntryKind.Fifo);
        var inspector = new FileSystemInspector(posix.Object);
        var tempDirectory = Directory.CreateDirectory(Path.Combine(TestDirectory.FullName, Guid.NewGuid().ToString("N")));
        var tempFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
        File.WriteAllText(tempFilePath, "x");
        var fileInfo = new FileInfo(tempFilePath);

        var result = inspector.ClassifyEntry(fileInfo);

        result.Should().Be(FileSystemEntryKind.Fifo);
    }

    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public void ClassifyEntry_FallsBackToRegularFile_WhenPosixClassifierThrows()
    {
        var posix = new Mock<IPosixFileTypeClassifier>(MockBehavior.Strict);
        posix.Setup(p => p.ClassifyPosixEntry(It.IsAny<string>())).Throws(new InvalidOperationException("boom"));
        var inspector = new FileSystemInspector(posix.Object);
        var tempDirectory = Directory.CreateDirectory(Path.Combine(TestDirectory.FullName, Guid.NewGuid().ToString("N")));
        var tempFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
        File.WriteAllText(tempFilePath, "x");
        var fileInfo = new FileInfo(tempFilePath);

        var result = inspector.ClassifyEntry(fileInfo);

        result.Should().Be(FileSystemEntryKind.RegularFile);
    }

    [Test]
    public void IsNoiseDirectoryName_ShouldReturnTrue_ForKnownNoiseDirectory()
    {
        var inspector = new FileSystemInspector();
        var noiseDirectory = Directory.CreateDirectory(Path.Combine(TestDirectory.FullName, "$RECYCLE.BIN"));

        var result = inspector.IsNoiseDirectoryName(noiseDirectory, OSPlatforms.Windows);

        result.Should().BeTrue();
    }

    [Test]
    public void IsNoiseDirectoryName_ShouldReturnFalse_ForUnknownDirectory()
    {
        var inspector = new FileSystemInspector();
        var regularDirectory = Directory.CreateDirectory(Path.Combine(TestDirectory.FullName, "regular"));

        var result = inspector.IsNoiseDirectoryName(regularDirectory, OSPlatforms.Windows);

        result.Should().BeFalse();
    }

    [Test]
    public void IsNoiseFileName_ShouldReturnTrue_ForKnownNoiseFile()
    {
        var inspector = new FileSystemInspector();
        var filePath = Path.Combine(TestDirectory.FullName, "thumbs.db");
        File.WriteAllText(filePath, "x");
        var fileInfo = new FileInfo(filePath);

        var result = inspector.IsNoiseFileName(fileInfo, OSPlatforms.Windows);

        result.Should().BeTrue();
    }

    [Test]
    public void IsNoiseFileName_ShouldReturnFalse_ForUnknownFile()
    {
        var inspector = new FileSystemInspector();
        var filePath = Path.Combine(TestDirectory.FullName, "regular.txt");
        File.WriteAllText(filePath, "x");
        var fileInfo = new FileInfo(filePath);

        var result = inspector.IsNoiseFileName(fileInfo, OSPlatforms.Windows);

        result.Should().BeFalse();
    }
}
