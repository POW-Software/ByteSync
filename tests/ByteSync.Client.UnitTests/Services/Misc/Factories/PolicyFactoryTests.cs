using System.Net;
using ByteSync.Exceptions;
using ByteSync.Services.Misc.Factories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Misc.Factories;

[TestFixture]
public class PolicyFactoryTests
{
    private Mock<ILogger<PolicyFactory>> _mockLogger = null!;
    private PolicyFactory _factory = null!;
    
    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<PolicyFactory>>();
        _factory = new PolicyFactory(_mockLogger.Object);
    }
    
    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    [TestCase(HttpStatusCode.BadGateway)]
    [TestCase(HttpStatusCode.GatewayTimeout)]
    [TestCase(HttpStatusCode.RequestTimeout)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public async Task BuildFileUploadPolicy_ShouldRetry_On_HttpRequestException_StatusCodes(HttpStatusCode status)
    {
        var policy = _factory.BuildFileUploadPolicy();
        
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(1000);
        
        Func<Task> act = async () =>
        {
            await policy.ExecuteAsync(async _ => { throw new HttpRequestException("test", inner: null, statusCode: status); },
                cts.Token);
        };
        
        await act.Should().ThrowAsync<OperationCanceledException>();
        
        _mockLogger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("FileTransferOperation failed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }
    
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    [TestCase(HttpStatusCode.BadGateway)]
    [TestCase(HttpStatusCode.GatewayTimeout)]
    [TestCase(HttpStatusCode.RequestTimeout)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public async Task BuildFileUploadPolicy_ShouldRetry_On_ApiException_StatusCodes(HttpStatusCode status)
    {
        var policy = _factory.BuildFileUploadPolicy();
        
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(1000);
        
        Func<Task> act = async () => { await policy.ExecuteAsync(async _ => { throw new ApiException("api error", status); }, cts.Token); };
        
        await act.Should().ThrowAsync<OperationCanceledException>();
        
        _mockLogger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("FileTransferOperation failed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }
    
    [Test]
    public async Task BuildFileUploadPolicy_ShouldNotRetry_On_Unhandled_HttpRequestException_Status()
    {
        var policy = _factory.BuildFileUploadPolicy();
        
        using var cts = new CancellationTokenSource();
        
        // no cancellation needed; exception is not handled and will bubble immediately
        
        Func<Task> act = async () =>
        {
            await policy.ExecuteAsync(
                async _ => { throw new HttpRequestException("not found", inner: null, statusCode: HttpStatusCode.NotFound); },
                cts.Token);
        };
        
        await act.Should().ThrowAsync<HttpRequestException>();
        
        _mockLogger.Verify(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }
}