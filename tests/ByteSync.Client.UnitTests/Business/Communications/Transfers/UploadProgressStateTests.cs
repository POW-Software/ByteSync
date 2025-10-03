using ByteSync.Business.Communications.Transfers;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Business.Communications.Transfers;

[TestFixture]
public class UploadProgressStateTests
{
    [Test]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var progressState = new UploadProgressState();
        
        // Assert
        progressState.Should().NotBeNull();
        progressState.TotalCreatedSlices.Should().Be(0);
        progressState.TotalUploadedSlices.Should().Be(0);
        progressState.ConcurrentUploads.Should().Be(0);
        progressState.MaxConcurrentUploads.Should().Be(0);
        progressState.Exceptions.Should().BeEmpty();
    }
    
    [Test]
    public void TotalCreatedSlices_ShouldBeGettableAndSettable()
    {
        // Arrange
        var progressState = new UploadProgressState();
        var expectedValue = 10;
        
        // Act
        progressState.TotalCreatedSlices = expectedValue;
        var result = progressState.TotalCreatedSlices;
        
        // Assert
        result.Should().Be(expectedValue);
    }
    
    [Test]
    public void TotalUploadedSlices_ShouldBeGettableAndSettable()
    {
        // Arrange
        var progressState = new UploadProgressState();
        var expectedValue = 5;
        
        // Act
        progressState.TotalUploadedSlices = expectedValue;
        var result = progressState.TotalUploadedSlices;
        
        // Assert
        result.Should().Be(expectedValue);
    }
    
    [Test]
    public void ConcurrentUploads_ShouldBeGettableAndSettable()
    {
        // Arrange
        var progressState = new UploadProgressState();
        var expectedValue = 3;
        
        // Act
        progressState.ConcurrentUploads = expectedValue;
        var result = progressState.ConcurrentUploads;
        
        // Assert
        result.Should().Be(expectedValue);
    }
    
    [Test]
    public void MaxConcurrentUploads_ShouldBeGettableAndSettable()
    {
        // Arrange
        var progressState = new UploadProgressState();
        var expectedValue = 8;
        
        // Act
        progressState.MaxConcurrentUploads = expectedValue;
        var result = progressState.MaxConcurrentUploads;
        
        // Assert
        result.Should().Be(expectedValue);
    }
    
    [Test]
    public void Exceptions_ShouldBeGettableAndSettable()
    {
        // Arrange
        var progressState = new UploadProgressState();
        var expectedException = new Exception("Test exception");
        
        // Act
        progressState.Exceptions.Add(expectedException);
        var result = progressState.Exceptions[0];
        
        // Assert
        result.Should().Be(expectedException);
    }
    
    [Test]
    public void TotalCreatedSlices_WithZeroValue_ShouldWork()
    {
        // Arrange
        var progressState = new UploadProgressState();
        
        // Act
        progressState.TotalCreatedSlices = 0;
        var result = progressState.TotalCreatedSlices;
        
        // Assert
        result.Should().Be(0);
    }
    
    [Test]
    public void TotalCreatedSlices_WithNegativeValue_ShouldWork()
    {
        // Arrange
        var progressState = new UploadProgressState();
        
        // Act
        progressState.TotalCreatedSlices = -5;
        var result = progressState.TotalCreatedSlices;
        
        // Assert
        result.Should().Be(-5);
    }
    
    [Test]
    public void TotalCreatedSlices_WithMaxValue_ShouldWork()
    {
        // Arrange
        var progressState = new UploadProgressState();
        
        // Act
        progressState.TotalCreatedSlices = int.MaxValue;
        var result = progressState.TotalCreatedSlices;
        
        // Assert
        result.Should().Be(int.MaxValue);
    }
    
    [Test]
    public void TotalCreatedSlices_WithMinValue_ShouldWork()
    {
        // Arrange
        var progressState = new UploadProgressState();
        
        // Act
        progressState.TotalCreatedSlices = int.MinValue;
        var result = progressState.TotalCreatedSlices;
        
        // Assert
        result.Should().Be(int.MinValue);
    }
    
    [Test]
    public void TotalUploadedSlices_WithZeroValue_ShouldWork()
    {
        // Arrange
        var progressState = new UploadProgressState();
        
        // Act
        progressState.TotalUploadedSlices = 0;
        var result = progressState.TotalUploadedSlices;
        
        // Assert
        result.Should().Be(0);
    }
    
    [Test]
    public void TotalUploadedSlices_WithNegativeValue_ShouldWork()
    {
        // Arrange
        var progressState = new UploadProgressState();
        
        // Act
        progressState.TotalUploadedSlices = -3;
        var result = progressState.TotalUploadedSlices;
        
        // Assert
        result.Should().Be(-3);
    }
    
    [Test]
    public void ConcurrentUploads_WithZeroValue_ShouldWork()
    {
        // Arrange
        var progressState = new UploadProgressState();
        
        // Act
        progressState.ConcurrentUploads = 0;
        var result = progressState.ConcurrentUploads;
        
        // Assert
        result.Should().Be(0);
    }
    
    [Test]
    public void ConcurrentUploads_WithNegativeValue_ShouldWork()
    {
        // Arrange
        var progressState = new UploadProgressState();
        
        // Act
        progressState.ConcurrentUploads = -1;
        var result = progressState.ConcurrentUploads;
        
        // Assert
        result.Should().Be(-1);
    }
    
    [Test]
    public void MaxConcurrentUploads_WithZeroValue_ShouldWork()
    {
        // Arrange
        var progressState = new UploadProgressState();
        
        // Act
        progressState.MaxConcurrentUploads = 0;
        var result = progressState.MaxConcurrentUploads;
        
        // Assert
        result.Should().Be(0);
    }
    
    [Test]
    public void MaxConcurrentUploads_WithNegativeValue_ShouldWork()
    {
        // Arrange
        var progressState = new UploadProgressState();
        
        // Act
        progressState.MaxConcurrentUploads = -10;
        var result = progressState.MaxConcurrentUploads;
        
        // Assert
        result.Should().Be(-10);
    }
    
    [Test]
    public void Exceptions_WithNullValue_ShouldWork()
    {
        // Arrange
        var progressState = new UploadProgressState();
        
        // Act
        
        progressState.Exceptions.Add(null!);
        var result = progressState.Exceptions[0];
        
        // Assert
        result.Should().BeNull();
    }
    
    [Test]
    public void Exceptions_WithDifferentExceptionTypes_ShouldWork()
    {
        // Arrange
        var progressState = new UploadProgressState();
        
        // Act & Assert
        var invalidOperationException = new InvalidOperationException("Invalid operation");
        progressState.Exceptions.Add(invalidOperationException);
        progressState.Exceptions[0].Should().Be(invalidOperationException);
        
        var argumentException = new ArgumentException("Invalid argument");
        progressState.Exceptions.Add(argumentException);
        progressState.Exceptions[1].Should().Be(argumentException);
        
        var timeoutException = new TimeoutException("Operation timed out");
        progressState.Exceptions.Add(timeoutException);
        progressState.Exceptions[2].Should().Be(timeoutException);
    }
    
    [Test]
    public void Properties_ShouldBeIndependent()
    {
        // Arrange
        var progressState = new UploadProgressState();
        
        // Act
        progressState.TotalCreatedSlices = 10;
        progressState.TotalUploadedSlices = 5;
        progressState.ConcurrentUploads = 3;
        progressState.MaxConcurrentUploads = 8;
        progressState.Exceptions.Add(new Exception("Test"));
        
        // Assert
        progressState.TotalCreatedSlices.Should().Be(10);
        progressState.TotalUploadedSlices.Should().Be(5);
        progressState.ConcurrentUploads.Should().Be(3);
        progressState.MaxConcurrentUploads.Should().Be(8);
        progressState.Exceptions.Should().NotBeNull();
    }
    
    [Test]
    public void MultipleInstances_ShouldBeIndependent()
    {
        // Arrange
        var progressState1 = new UploadProgressState();
        var progressState2 = new UploadProgressState();
        
        // Act
        progressState1.TotalCreatedSlices = 10;
        progressState2.TotalCreatedSlices = 20;
        
        // Assert
        progressState1.TotalCreatedSlices.Should().Be(10);
        progressState2.TotalCreatedSlices.Should().Be(20);
    }
    
    [Test]
    public void Properties_ShouldBeMutable()
    {
        // Arrange
        var progressState = new UploadProgressState();
        
        // Act & Assert
        progressState.TotalCreatedSlices = 1;
        progressState.TotalCreatedSlices.Should().Be(1);
        
        progressState.TotalCreatedSlices = 2;
        progressState.TotalCreatedSlices.Should().Be(2);
        
        progressState.TotalCreatedSlices = 3;
        progressState.TotalCreatedSlices.Should().Be(3);
    }
    
    [Test]
    public void Exceptions_ShouldPreserveInnerException()
    {
        // Arrange
        var progressState = new UploadProgressState();
        var innerException = new Exception("Inner exception");
        var outerException = new Exception("Outer exception", innerException);
        
        // Act
        progressState.Exceptions.Add(outerException);
        
        // Assert
        progressState.Exceptions[0].Should().Be(outerException);
        progressState.Exceptions[0].InnerException.Should().Be(innerException);
    }
    
    [Test]
    public void LastException_WithExceptionWithMessage_ShouldPreserveMessage()
    {
        // Arrange
        var progressState = new UploadProgressState();
        var exceptionMessage = "This is a test exception message";
        var exception = new Exception(exceptionMessage);
        
        // Act
        progressState.Exceptions.Add(exception);
        
        // Assert
        progressState.Exceptions[0].Should().Be(exception);
        progressState.Exceptions[0].Message.Should().Be(exceptionMessage);
    }
    
    [Test]
    public void LastException_WithExceptionWithStackTrace_ShouldPreserveStackTrace()
    {
        // Arrange
        var progressState = new UploadProgressState();
        Exception exception;
        try
        {
            throw new Exception("Test exception");
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        
        // Act
        progressState.Exceptions.Add(exception);
        
        // Assert
        progressState.Exceptions[0].Should().Be(exception);
        progressState.Exceptions[0].StackTrace.Should().NotBeNullOrEmpty();
    }
}