using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Services.Communications.Api;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Api;

[TestFixture]
public class TrustApiClientTests
{
    private Mock<IApiInvoker> _mockApiInvoker = null!;
    private Mock<ILogger<TrustApiClient>> _mockLogger = null!;
    private TrustApiClient _trustApiClient = null!;
    
    [SetUp]
    public void SetUp()
    {
        _mockApiInvoker = new Mock<IApiInvoker>();
        _mockLogger = new Mock<ILogger<TrustApiClient>>();
        
        _trustApiClient = new TrustApiClient(
            _mockApiInvoker.Object,
            _mockLogger.Object);
    }
    
    [Test]
    public async Task InformProtocolVersionIncompatible_WithValidParameters_ShouldCallApiInvokerWithCorrectParameters()
    {
        var parameters = new InformProtocolVersionIncompatibleParameters
        {
            SessionId = "test-session-id",
            MemberClientInstanceId = "member-instance-id",
            JoinerClientInstanceId = "joiner-instance-id",
            MemberProtocolVersion = ProtocolVersion.CURRENT,
            JoinerProtocolVersion = 0
        };
        
        _mockApiInvoker
            .Setup(x => x.PostAsync("trust/informProtocolVersionIncompatible", parameters, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        await _trustApiClient.InformProtocolVersionIncompatible(parameters);
        
        _mockApiInvoker.Verify(
            x => x.PostAsync("trust/informProtocolVersionIncompatible", parameters, It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Test]
    public async Task InformProtocolVersionIncompatible_WhenApiInvokerThrows_ShouldRethrowException()
    {
        var parameters = new InformProtocolVersionIncompatibleParameters
        {
            SessionId = "test-session-id",
            MemberClientInstanceId = "member-instance-id",
            JoinerClientInstanceId = "joiner-instance-id",
            MemberProtocolVersion = ProtocolVersion.CURRENT,
            JoinerProtocolVersion = 0
        };
        var expectedException = new Exception("API error");
        
        _mockApiInvoker
            .Setup(x => x.PostAsync("trust/informProtocolVersionIncompatible", parameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);
        
        Func<Task> act = async () => await _trustApiClient.InformProtocolVersionIncompatible(parameters);
        
        await act.Should().ThrowAsync<Exception>().WithMessage("API error");
    }
}