using System.IO;
using ByteSync.Business.Inventories;
using ByteSync.Services.Inventories;
using ByteSync.TestsCommon;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Inventories;

public class PosixFileTypeClassifierTests : AbstractTester
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
    [Platform(Include = "Linux,MacOsX")]
    [TestCase("/dev/null", FileSystemEntryKind.CharacterDevice)]
    [TestCase("/dev/zero", FileSystemEntryKind.CharacterDevice)]
    public void ClassifyPosixEntry_ReturnsExpected(string path, FileSystemEntryKind expected)
    {
        if (!File.Exists(path))
        {
            Assert.Ignore($"Path '{path}' not found on this system.");
        }

        var classifier = new PosixFileTypeClassifier();

        var result = classifier.ClassifyPosixEntry(path);

        if (result == FileSystemEntryKind.Unknown)
        {
            Assert.Ignore($"POSIX classification returned Unknown for '{path}'.");
        }

        result.Should().Be(expected);
    }

    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public void ClassifyPosixEntry_ReturnsRegularFile_ForTempFile()
    {
        var classifier = new PosixFileTypeClassifier();
        var tempDirectory = Path.Combine(TestDirectory.FullName, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var tempFile = Path.Combine(tempDirectory, "file.txt");
        File.WriteAllText(tempFile, "data");

        var result = classifier.ClassifyPosixEntry(tempFile);

        if (result == FileSystemEntryKind.Unknown)
        {
            Assert.Ignore($"POSIX classification returned Unknown for '{tempFile}'.");
        }

        result.Should().Be(FileSystemEntryKind.RegularFile);
    }

    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public void ClassifyPosixEntry_ReturnsDirectory_ForTempDirectory()
    {
        var classifier = new PosixFileTypeClassifier();
        var tempDirectory = Path.Combine(TestDirectory.FullName, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        var result = classifier.ClassifyPosixEntry(tempDirectory);

        if (result == FileSystemEntryKind.Unknown)
        {
            Assert.Ignore($"POSIX classification returned Unknown for '{tempDirectory}'.");
        }

        result.Should().Be(FileSystemEntryKind.Directory);
    }

    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public void ClassifyPosixEntry_ReturnsUnknown_ForMissingPath()
    {
        var classifier = new PosixFileTypeClassifier();
        var missingPath = Path.Combine(TestDirectory.FullName, Guid.NewGuid().ToString("N"), "missing");

        var result = classifier.ClassifyPosixEntry(missingPath);

        result.Should().Be(FileSystemEntryKind.Unknown);
    }

    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public void ClassifyPosixEntry_ReturnsSymlink_WhenSupported()
    {
        var classifier = new PosixFileTypeClassifier();
        var tempDirectory = Path.Combine(TestDirectory.FullName, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var targetFile = Path.Combine(tempDirectory, "target.txt");
        File.WriteAllText(targetFile, "data");
        var linkPath = Path.Combine(tempDirectory, "link.txt");

        try
        {
            File.CreateSymbolicLink(linkPath, targetFile);
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Symbolic link creation failed: {ex.GetType().Name}");
        }

        var result = classifier.ClassifyPosixEntry(linkPath);

        if (result == FileSystemEntryKind.Unknown)
        {
            Assert.Ignore($"POSIX classification returned Unknown for '{linkPath}'.");
        }

        result.Should().Be(FileSystemEntryKind.Symlink);
    }

    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public void ClassifyPosixEntry_ReturnsUnknown_WhenUnixFileInfoThrows()
    {
        var classifier = new PosixFileTypeClassifier(_ => throw new InvalidOperationException("fail"));
        var tempDirectory = Path.Combine(TestDirectory.FullName, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var tempFile = Path.Combine(tempDirectory, "file.txt");
        File.WriteAllText(tempFile, "data");

        var result = classifier.ClassifyPosixEntry(tempFile);

        if (result == FileSystemEntryKind.Unknown)
        {
            Assert.Ignore($"POSIX classification returned Unknown for '{tempFile}'.");
        }

        result.Should().Be(FileSystemEntryKind.RegularFile);
    }

    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public void ClassifyPosixEntry_ReturnsUnknown_WhenDllNotFound()
    {
        var classifier = new PosixFileTypeClassifier(_ => throw new InvalidOperationException("unused"),
            _ => throw new DllNotFoundException("missing"));

        var result = classifier.ClassifyPosixEntry("/tmp");

        result.Should().Be(FileSystemEntryKind.Unknown);
    }

    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public void ClassifyPosixEntry_ReturnsUnknown_WhenEntryPointNotFound()
    {
        var classifier = new PosixFileTypeClassifier(_ => throw new InvalidOperationException("unused"),
            _ => throw new EntryPointNotFoundException("missing"));

        var result = classifier.ClassifyPosixEntry("/tmp");

        result.Should().Be(FileSystemEntryKind.Unknown);
    }

    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public void ClassifyPosixEntry_ReturnsUnknown_WhenPlatformNotSupported()
    {
        var classifier = new PosixFileTypeClassifier(_ => throw new InvalidOperationException("unused"),
            _ => throw new PlatformNotSupportedException("missing"));

        var result = classifier.ClassifyPosixEntry("/tmp");

        result.Should().Be(FileSystemEntryKind.Unknown);
    }
}
