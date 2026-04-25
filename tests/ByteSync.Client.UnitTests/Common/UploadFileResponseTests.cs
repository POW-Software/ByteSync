using ByteSync.Common.Business.Communications.Transfers;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Common;

[TestFixture]
public class UploadFileResponseTests
{
    [Test]
    public void Success_ShouldHaveNoFailureKind()
    {
        var response = UploadFileResponse.Success(204);
        
        response.IsSuccess.Should().BeTrue();
        response.StatusCode.Should().Be(204);
        response.FailureKind.Should().Be(UploadFailureKind.None);
        response.Exception.Should().BeNull();
        response.ErrorMessage.Should().BeNull();
    }
    
    [Test]
    public void FailureWithMessage_ShouldBeServerError()
    {
        var response = UploadFileResponse.Failure(503, "Service unavailable");
        
        response.IsSuccess.Should().BeFalse();
        response.StatusCode.Should().Be(503);
        response.ErrorMessage.Should().Be("Service unavailable");
        response.FailureKind.Should().Be(UploadFailureKind.ServerError);
        response.Exception.Should().BeNull();
    }
    
    [Test]
    public void FailureWithException_ShouldBeServerError_AndCaptureException()
    {
        var ex = new InvalidOperationException("boom");
        
        var response = UploadFileResponse.Failure(500, ex);
        
        response.IsSuccess.Should().BeFalse();
        response.StatusCode.Should().Be(500);
        response.ErrorMessage.Should().Be("boom");
        response.Exception.Should().BeSameAs(ex);
        response.FailureKind.Should().Be(UploadFailureKind.ServerError);
    }
    
    [Test]
    public void ClientCancellation_ShouldNotHaveServerStatusCode()
    {
        var ex = new OperationCanceledException("canceled");
        
        var response = UploadFileResponse.ClientCancellation(ex);
        
        response.IsSuccess.Should().BeFalse();
        response.StatusCode.Should().Be(0);
        response.ErrorMessage.Should().Be("canceled");
        response.Exception.Should().BeSameAs(ex);
        response.FailureKind.Should().Be(UploadFailureKind.ClientCancellation);
    }
    
    [Test]
    public void ClientTimeout_ShouldBeDistinctFromCancellation()
    {
        var ex = new TaskCanceledException("timed out");
        
        var response = UploadFileResponse.ClientTimeout(ex);
        
        response.IsSuccess.Should().BeFalse();
        response.StatusCode.Should().Be(0);
        response.ErrorMessage.Should().Be("timed out");
        response.Exception.Should().BeSameAs(ex);
        response.FailureKind.Should().Be(UploadFailureKind.ClientTimeout);
    }
    
    [Test]
    public void FailureKind_DefaultValueOnNewInstance_ShouldBeNone()
    {
        var response = new UploadFileResponse();
        
        response.FailureKind.Should().Be(UploadFailureKind.None);
    }
}
