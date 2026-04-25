using System.Net;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Services.Communications.Transfers.Strategies;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Strategies;

[TestFixture]
public class CloudflareR2UploadStrategyTests
{
    private const string UploadUrl = "https://test-bucket.r2.cloudflarestorage.com/test/slice-1";
    
    private static FileUploaderSlice CreateSlice(int partNumber = 1, int sizeBytes = 64)
    {
        var bytes = new byte[sizeBytes];
        Array.Fill<byte>(bytes, 0x42);
        return new FileUploaderSlice(partNumber, new MemoryStream(bytes, writable: true));
    }
    
    private static FileStorageLocation CreateLocation() => new(UploadUrl, StorageProvider.CloudflareR2);
    
    private static (CloudflareR2UploadStrategy strategy, Mock<HttpMessageHandler> handler) CreateStrategy()
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var factory = new Mock<IHttpClientFactory>();
        factory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler.Object, disposeHandler: false));
        
        var strategy = new CloudflareR2UploadStrategy(NullLogger<CloudflareR2UploadStrategy>.Instance, factory.Object);
        return (strategy, handler);
    }
    
    private static void SetupHandler(Mock<HttpMessageHandler> handler, HttpResponseMessage response)
    {
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
    
    private static void SetupHandlerThrows(Mock<HttpMessageHandler> handler, Exception exception)
    {
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);
    }
    
    [Test]
    public async Task UploadAsync_On2xx_ShouldReturnSuccess()
    {
        var (strategy, handler) = CreateStrategy();
        SetupHandler(handler, new HttpResponseMessage(HttpStatusCode.OK));
        
        var response = await strategy.UploadAsync(CreateSlice(), CreateLocation(), CancellationToken.None);
        
        response.IsSuccess.Should().BeTrue();
        response.StatusCode.Should().Be(200);
        response.FailureKind.Should().Be(UploadFailureKind.None);
    }
    
    [Test]
    public async Task UploadAsync_On500_ShouldReturnServerFailure_WithRealStatusCode()
    {
        var (strategy, handler) = CreateStrategy();
        SetupHandler(handler, new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("boom")
        });
        
        var response = await strategy.UploadAsync(CreateSlice(), CreateLocation(), CancellationToken.None);
        
        response.IsSuccess.Should().BeFalse();
        response.StatusCode.Should().Be(500);
        response.FailureKind.Should().Be(UploadFailureKind.ServerError);
    }
    
    [Test]
    public async Task UploadAsync_On429_ShouldReturnServerFailure_WithRealStatusCode()
    {
        var (strategy, handler) = CreateStrategy();
        SetupHandler(handler, new HttpResponseMessage((HttpStatusCode)429)
        {
            Content = new StringContent("rate limited")
        });
        
        var response = await strategy.UploadAsync(CreateSlice(), CreateLocation(), CancellationToken.None);
        
        response.IsSuccess.Should().BeFalse();
        response.StatusCode.Should().Be(429);
        response.FailureKind.Should().Be(UploadFailureKind.ServerError);
    }
    
    [Test]
    public async Task UploadAsync_WhenCallerCancels_ShouldReturnClientCancellation()
    {
        var factory = new Mock<IHttpClientFactory>();
        var hangingHandler = new HangingHttpMessageHandler();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(hangingHandler, disposeHandler: false));
        var strategy = new CloudflareR2UploadStrategy(NullLogger<CloudflareR2UploadStrategy>.Instance, factory.Object);
        
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        
        var response = await strategy.UploadAsync(CreateSlice(), CreateLocation(), cts.Token);
        
        response.IsSuccess.Should().BeFalse();
        response.FailureKind.Should().Be(UploadFailureKind.ClientCancellation);
        response.StatusCode.Should().Be(0);
        response.Exception.Should().BeAssignableTo<OperationCanceledException>();
    }
    
    private sealed class HangingHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
    
    [Test]
    public async Task UploadAsync_WhenHttpRequestExceptionThrown_ShouldReturnServerError500()
    {
        var (strategy, handler) = CreateStrategy();
        SetupHandlerThrows(handler, new HttpRequestException("socket reset"));
        
        var response = await strategy.UploadAsync(CreateSlice(), CreateLocation(), CancellationToken.None);
        
        response.IsSuccess.Should().BeFalse();
        response.FailureKind.Should().Be(UploadFailureKind.ServerError);
        response.StatusCode.Should().Be(500);
        response.Exception.Should().BeAssignableTo<HttpRequestException>();
    }
    
    [Test]
    public async Task UploadAsync_WhenIOExceptionThrown_ShouldReturnServerError500()
    {
        var (strategy, handler) = CreateStrategy();
        SetupHandlerThrows(handler, new IOException("broken pipe"));
        
        var response = await strategy.UploadAsync(CreateSlice(), CreateLocation(), CancellationToken.None);
        
        response.IsSuccess.Should().BeFalse();
        response.FailureKind.Should().Be(UploadFailureKind.ServerError);
        response.StatusCode.Should().Be(500);
    }
    
    [Test]
    public async Task UploadAsync_WhenOperationCanceledThrown_WithCancelledToken_ShouldReturnClientCancellation()
    {
        var (strategy, handler) = CreateStrategy();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        SetupHandlerThrows(handler, new OperationCanceledException("cancel"));
        
        var response = await strategy.UploadAsync(CreateSlice(), CreateLocation(), cts.Token);
        
        response.IsSuccess.Should().BeFalse();
        response.FailureKind.Should().Be(UploadFailureKind.ClientCancellation);
        response.StatusCode.Should().Be(0);
    }
    
    [Test]
    public async Task UploadAsync_WhenOperationCanceledThrown_WithLiveToken_ShouldReturnServerError()
    {
        var (strategy, handler) = CreateStrategy();
        using var cts = new CancellationTokenSource();
        SetupHandlerThrows(handler, new OperationCanceledException("odd"));
        
        var response = await strategy.UploadAsync(CreateSlice(), CreateLocation(), cts.Token);
        
        response.IsSuccess.Should().BeFalse();
        response.FailureKind.Should().Be(UploadFailureKind.ServerError);
        response.StatusCode.Should().Be(500);
    }
}
