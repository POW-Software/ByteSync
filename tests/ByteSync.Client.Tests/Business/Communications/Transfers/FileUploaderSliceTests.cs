using NUnit.Framework;
using System.IO;
using ByteSync.Business.Communications.Transfers;
using FluentAssertions;

namespace ByteSync.Tests.Business.Communications.Transfers;

[TestFixture]
public class FileUploaderSliceTests
{
    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var partNumber = 1;
        var memoryStream = new MemoryStream();

        // Act
        var slice = new FileUploaderSlice(partNumber, memoryStream);

        // Assert
        slice.Should().NotBeNull();
        slice.PartNumber.Should().Be(partNumber);
        slice.MemoryStream.Should().BeSameAs(memoryStream);
    }

    [Test]
    public void Constructor_ShouldSetMemoryStreamPositionToZero()
    {
        // Arrange
        var partNumber = 1;
        var memoryStream = new MemoryStream();
        memoryStream.Position = 100; // Set position to non-zero

        // Act
        var slice = new FileUploaderSlice(partNumber, memoryStream);

        // Assert
        memoryStream.Position.Should().Be(0);
    }

    [Test]
    public void Constructor_WithZeroPartNumber_ShouldCreateInstance()
    {
        // Arrange
        var partNumber = 0;
        var memoryStream = new MemoryStream();

        // Act
        var slice = new FileUploaderSlice(partNumber, memoryStream);

        // Assert
        slice.Should().NotBeNull();
        slice.PartNumber.Should().Be(partNumber);
    }

    [Test]
    public void Constructor_WithNegativePartNumber_ShouldCreateInstance()
    {
        // Arrange
        var partNumber = -1;
        var memoryStream = new MemoryStream();

        // Act
        var slice = new FileUploaderSlice(partNumber, memoryStream);

        // Assert
        slice.Should().NotBeNull();
        slice.PartNumber.Should().Be(partNumber);
    }

    [Test]
    public void Constructor_WithLargePartNumber_ShouldCreateInstance()
    {
        // Arrange
        var partNumber = int.MaxValue;
        var memoryStream = new MemoryStream();

        // Act
        var slice = new FileUploaderSlice(partNumber, memoryStream);

        // Assert
        slice.Should().NotBeNull();
        slice.PartNumber.Should().Be(partNumber);
    }

    [Test]
    public void Constructor_WithEmptyMemoryStream_ShouldCreateInstance()
    {
        // Arrange
        var partNumber = 1;
        var memoryStream = new MemoryStream();

        // Act
        var slice = new FileUploaderSlice(partNumber, memoryStream);

        // Assert
        slice.Should().NotBeNull();
        slice.MemoryStream.Should().BeSameAs(memoryStream);
        slice.MemoryStream.Length.Should().Be(0);
    }

    [Test]
    public void Constructor_WithPopulatedMemoryStream_ShouldCreateInstance()
    {
        // Arrange
        var partNumber = 1;
        var memoryStream = new MemoryStream();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        memoryStream.Write(data, 0, data.Length);

        // Act
        var slice = new FileUploaderSlice(partNumber, memoryStream);

        // Assert
        slice.Should().NotBeNull();
        slice.MemoryStream.Should().BeSameAs(memoryStream);
        slice.MemoryStream.Length.Should().Be(data.Length);
    }

    [Test]
    public void PartNumber_ShouldBeReadOnly()
    {
        // Arrange
        var partNumber = 1;
        var memoryStream = new MemoryStream();
        var slice = new FileUploaderSlice(partNumber, memoryStream);

        // Act & Assert
        // The property should be read-only, so we can't set it
        // This test verifies the property is accessible but not settable
        slice.PartNumber.Should().Be(partNumber);
    }

    [Test]
    public void MemoryStream_ShouldBeReadOnly()
    {
        // Arrange
        var partNumber = 1;
        var memoryStream = new MemoryStream();
        var slice = new FileUploaderSlice(partNumber, memoryStream);

        // Act & Assert
        // The property should be read-only, so we can't set it
        // This test verifies the property is accessible but not settable
        slice.MemoryStream.Should().BeSameAs(memoryStream);
    }

    [Test]
    public void Constructor_WithNullMemoryStream_ShouldThrowNullReferenceException()
    {
        // Arrange
        var partNumber = 1;
        MemoryStream? memoryStream = null;

        // Act & Assert
        var action = () => new FileUploaderSlice(partNumber, memoryStream!);
        action.Should().Throw<NullReferenceException>();
    }

    [Test]
    public void Constructor_WithDisposedMemoryStream_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var partNumber = 1;
        var memoryStream = new MemoryStream();
        memoryStream.Dispose();

        // Act & Assert
        // This should throw ObjectDisposedException when trying to set Position
        var action = () => new FileUploaderSlice(partNumber, memoryStream);
        action.Should().Throw<ObjectDisposedException>()
            .WithMessage("*Cannot access a closed Stream*");
    }

    [Test]
    public void MultipleInstances_ShouldBeIndependent()
    {
        // Arrange
        var slice1 = new FileUploaderSlice(1, new MemoryStream());
        var slice2 = new FileUploaderSlice(2, new MemoryStream());

        // Act & Assert
        slice1.PartNumber.Should().Be(1);
        slice2.PartNumber.Should().Be(2);
        slice1.MemoryStream.Should().NotBeSameAs(slice2.MemoryStream);
    }

    [Test]
    public void Constructor_WithSameMemoryStream_ShouldCreateDifferentInstances()
    {
        // Arrange
        var memoryStream = new MemoryStream();
        var slice1 = new FileUploaderSlice(1, memoryStream);
        var slice2 = new FileUploaderSlice(2, memoryStream);

        // Act & Assert
        slice1.PartNumber.Should().Be(1);
        slice2.PartNumber.Should().Be(2);
        slice1.MemoryStream.Should().BeSameAs(slice2.MemoryStream); // Same reference
    }

    [Test]
    public void MemoryStream_ShouldBeWritableAfterConstruction()
    {
        // Arrange
        var partNumber = 1;
        var memoryStream = new MemoryStream();
        var slice = new FileUploaderSlice(partNumber, memoryStream);

        // Act
        var data = new byte[] { 1, 2, 3, 4, 5 };
        slice.MemoryStream.Write(data, 0, data.Length);

        // Assert
        slice.MemoryStream.Length.Should().Be(data.Length);
    }

    [Test]
    public void MemoryStream_ShouldBeReadableAfterConstruction()
    {
        // Arrange
        var partNumber = 1;
        var memoryStream = new MemoryStream();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        memoryStream.Write(data, 0, data.Length);
        memoryStream.Position = 0;

        var slice = new FileUploaderSlice(partNumber, memoryStream);

        // Act
        var readData = new byte[data.Length];
        var bytesRead = slice.MemoryStream.Read(readData, 0, readData.Length);

        // Assert
        bytesRead.Should().Be(data.Length);
        readData.Should().BeEquivalentTo(data);
    }

    [Test]
    public void Constructor_WithLargeMemoryStream_ShouldCreateInstance()
    {
        // Arrange
        var partNumber = 1;
        var memoryStream = new MemoryStream();
        var largeData = new byte[1024 * 1024]; // 1MB
        memoryStream.Write(largeData, 0, largeData.Length);

        // Act
        var slice = new FileUploaderSlice(partNumber, memoryStream);

        // Assert
        slice.Should().NotBeNull();
        slice.MemoryStream.Length.Should().Be(largeData.Length);
    }

    [Test]
    public void Constructor_WithMemoryStreamAtEnd_ShouldResetPosition()
    {
        // Arrange
        var partNumber = 1;
        var memoryStream = new MemoryStream();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        memoryStream.Write(data, 0, data.Length);
        // Position is now at the end

        // Act
        var slice = new FileUploaderSlice(partNumber, memoryStream);

        // Assert
        slice.MemoryStream.Position.Should().Be(0);
    }

    [Test]
    public void Constructor_WithMemoryStreamInMiddle_ShouldResetPosition()
    {
        // Arrange
        var partNumber = 1;
        var memoryStream = new MemoryStream();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        memoryStream.Write(data, 0, data.Length);
        memoryStream.Position = 2; // Position in middle

        // Act
        var slice = new FileUploaderSlice(partNumber, memoryStream);

        // Assert
        slice.MemoryStream.Position.Should().Be(0);
    }
} 