using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Services.Communications.Transfers.Strategies;
using FluentAssertions;
using NUnit.Framework;
using System.Net.Http;
using System.Net.Sockets;

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Strategies;

[TestFixture]
public class UploadFailureClassifierTests
{
    [Test]
    public void Classify_OperationCanceledException_WithCancelledToken_ShouldReturnClientCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var ex = new OperationCanceledException("user cancel");
        
        var response = UploadFailureClassifier.Classify(ex, cts.Token);
        
        response.IsSuccess.Should().BeFalse();
        response.FailureKind.Should().Be(UploadFailureKind.ClientCancellation);
        response.Exception.Should().BeSameAs(ex);
    }
    
    [Test]
    public void Classify_TaskCanceledException_WithCancelledToken_ShouldReturnClientCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var ex = new TaskCanceledException("timed out");
        
        var response = UploadFailureClassifier.Classify(ex, cts.Token);
        
        response.IsSuccess.Should().BeFalse();
        response.FailureKind.Should().Be(UploadFailureKind.ClientCancellation);
        response.Exception.Should().BeSameAs(ex);
    }
    
    [Test]
    public void Classify_OperationCanceledException_WithNonCancelledToken_ShouldReturnClientTimeout()
    {
        using var cts = new CancellationTokenSource();
        var ex = new OperationCanceledException("odd");
        
        var response = UploadFailureClassifier.Classify(ex, cts.Token);
        
        response.IsSuccess.Should().BeFalse();
        response.FailureKind.Should().Be(UploadFailureKind.ClientTimeout);
        response.StatusCode.Should().Be(0);
        response.Exception.Should().BeSameAs(ex);
    }
    
    [Test]
    public void Classify_TaskCanceledException_WithNonCancelledToken_ShouldReturnClientTimeout()
    {
        using var cts = new CancellationTokenSource();
        var ex = new TaskCanceledException("http timeout");
        
        var response = UploadFailureClassifier.Classify(ex, cts.Token);
        
        response.IsSuccess.Should().BeFalse();
        response.FailureKind.Should().Be(UploadFailureKind.ClientTimeout);
        response.StatusCode.Should().Be(0);
        response.Exception.Should().BeSameAs(ex);
    }
    
    [Test]
    public void Classify_GenericException_ShouldReturnServerError500()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var ex = new InvalidOperationException("broken");
        
        var response = UploadFailureClassifier.Classify(ex, cts.Token);
        
        response.IsSuccess.Should().BeFalse();
        response.StatusCode.Should().Be(500);
        response.FailureKind.Should().Be(UploadFailureKind.ServerError);
        response.Exception.Should().BeSameAs(ex);
    }
    
    [Test]
    public void Classify_HttpRequestExceptionWithoutSocketFailure_ShouldReturnServerError500()
    {
        using var cts = new CancellationTokenSource();
        var ex = new HttpRequestException("network issue");

        var response = UploadFailureClassifier.Classify(ex, cts.Token);

        response.IsSuccess.Should().BeFalse();
        response.StatusCode.Should().Be(500);
        response.FailureKind.Should().Be(UploadFailureKind.ServerError);
        response.Exception.Should().BeSameAs(ex);
    }

    [Test]
    public void Classify_HttpRequestExceptionWithConnectionReset_ShouldReturnClientNetworkError()
    {
        using var cts = new CancellationTokenSource();
        var socketException = new SocketException((int)SocketError.ConnectionReset);
        var ioException = new IOException("transport closed", socketException);
        var ex = new HttpRequestException("copy failed", ioException);

        var response = UploadFailureClassifier.Classify(ex, cts.Token);

        response.IsSuccess.Should().BeFalse();
        response.StatusCode.Should().Be(0);
        response.FailureKind.Should().Be(UploadFailureKind.ClientNetworkError);
        response.Exception.Should().BeSameAs(ex);
    }
}
