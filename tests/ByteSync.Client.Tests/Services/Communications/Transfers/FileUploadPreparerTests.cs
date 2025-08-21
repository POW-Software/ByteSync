using NUnit.Framework;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Services.Communications.Transfers;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;

namespace ByteSync.Tests.Services.Communications.Transfers;

[TestFixture]
public class FileUploadPreparerTests
{
    private IFileUploadPreparer _fileUploadPreparer;
    private SharedFileDefinition _sharedFileDefinition;

    [SetUp]
    public void SetUp()
    {
        _fileUploadPreparer = new FileUploadPreparer();
        _sharedFileDefinition = new SharedFileDefinition
        {
            Id = "test-file-id",
            SessionId = "test-session-id",
            UploadedFileLength = 0
        };
    }

    [Test]
    public void Constructor_ShouldCreateInstance()
    {
        // Act
        var preparer = new FileUploadPreparer();

        // Assert
        preparer.Should().NotBeNull();
        preparer.Should().BeAssignableTo<IFileUploadPreparer>();
    }

    [Test]
    public void PrepareUpload_ShouldSetIV()
    {
        // Arrange
        var originalIV = _sharedFileDefinition.IV;
        var fileLength = 1024L;

        // Act
        _fileUploadPreparer.PrepareUpload(_sharedFileDefinition, fileLength);

        // Assert
        _sharedFileDefinition.IV.Should().NotBeNull();
        if (originalIV != null)
        {
            _sharedFileDefinition.IV.Should().NotBeEquivalentTo(originalIV);
        }
    }

    [Test]
    public void PrepareUpload_ShouldSetFileLength()
    {
        // Arrange
        var fileLength = 2048L;

        // Act
        _fileUploadPreparer.PrepareUpload(_sharedFileDefinition, fileLength);

        // Assert
        _sharedFileDefinition.UploadedFileLength.Should().Be(fileLength);
    }

    [Test]
    public void PrepareUpload_WithZeroLength_ShouldSetZeroLength()
    {
        // Arrange
        var fileLength = 0L;

        // Act
        _fileUploadPreparer.PrepareUpload(_sharedFileDefinition, fileLength);

        // Assert
        _sharedFileDefinition.UploadedFileLength.Should().Be(0);
    }

    [Test]
    public void PrepareUpload_WithLargeLength_ShouldSetLargeLength()
    {
        // Arrange
        var fileLength = long.MaxValue;

        // Act
        _fileUploadPreparer.PrepareUpload(_sharedFileDefinition, fileLength);

        // Assert
        _sharedFileDefinition.UploadedFileLength.Should().Be(long.MaxValue);
    }

    [Test]
    public void PrepareUpload_WithNegativeLength_ShouldSetNegativeLength()
    {
        // Arrange
        var fileLength = -1024L;

        // Act
        _fileUploadPreparer.PrepareUpload(_sharedFileDefinition, fileLength);

        // Assert
        _sharedFileDefinition.UploadedFileLength.Should().Be(-1024);
    }

    [Test]
    public void PrepareUpload_ShouldGenerateUniqueIVs()
    {
        // Arrange
        var fileLength = 1024L;
        var firstIV = new byte[16];
        var secondIV = new byte[16];

        // Act
        _fileUploadPreparer.PrepareUpload(_sharedFileDefinition, fileLength);
        firstIV = _sharedFileDefinition.IV;

        var secondSharedFileDefinition = new SharedFileDefinition
        {
            Id = "test-file-id-2",
            SessionId = "test-session-id",
            UploadedFileLength = 0
        };

        _fileUploadPreparer.PrepareUpload(secondSharedFileDefinition, fileLength);
        secondIV = secondSharedFileDefinition.IV;

        // Assert
        firstIV.Should().NotBeEquivalentTo(secondIV);
    }

    [Test]
    public void PrepareUpload_WithNullSharedFileDefinition_ShouldThrowNullReferenceException()
    {
        // Act & Assert
        var action = () => _fileUploadPreparer.PrepareUpload(null!, 1024L);
        action.Should().Throw<NullReferenceException>();
    }

    [Test]
    public void PrepareUpload_MultipleCalls_ShouldUpdateValues()
    {
        // Arrange
        var firstLength = 1024L;
        var secondLength = 2048L;

        // Act
        _fileUploadPreparer.PrepareUpload(_sharedFileDefinition, firstLength);
        var firstIV = _sharedFileDefinition.IV;
        var firstLengthResult = _sharedFileDefinition.UploadedFileLength;

        _fileUploadPreparer.PrepareUpload(_sharedFileDefinition, secondLength);
        var secondIV = _sharedFileDefinition.IV;
        var secondLengthResult = _sharedFileDefinition.UploadedFileLength;

        // Assert
        firstLengthResult.Should().Be(firstLength);
        secondLengthResult.Should().Be(secondLength);
        firstIV.Should().NotBeEquivalentTo(secondIV);
    }
} 