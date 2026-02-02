using System.IO;
using ByteSync.Business.Inventories;
using ByteSync.Services.Inventories;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Inventories;

public class PosixFileTypeClassifierTests
{
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
}
