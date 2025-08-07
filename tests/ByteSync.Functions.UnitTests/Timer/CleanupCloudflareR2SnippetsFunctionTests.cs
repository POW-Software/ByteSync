using ByteSync.ServerCommon.Commands.Storage;
using ByteSync.Functions.Timer;
using Moq;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace ByteSync.Functions.UnitTests.Timer;

[TestFixture]
public class CleanupCloudflareR2SnippetsFunctionTests
{
    private Mock<IMediator> _mediator = null!;
    private Mock<ILogger<CleanupCloudflareR2SnippetsFunction>> _logger = null!;
    private Mock<IConfiguration> _configuration = null!;
    private CleanupCloudflareR2SnippetsFunction _function = null!;

    [SetUp]
    public void Setup()
    {
        _mediator = new Mock<IMediator>();
        _logger = new Mock<ILogger<CleanupCloudflareR2SnippetsFunction>>();
        _configuration = new Mock<IConfiguration>();
        
        _function = new CleanupCloudflareR2SnippetsFunction(_configuration.Object, _mediator.Object, _logger.Object);
    }

    [Test]
    public async Task RunAsync_ShouldSendCleanupRequest()
    {
        // Arrange
        var expectedDeletedCount = 5;
        _mediator.Setup(m => m.Send(It.IsAny<CleanupCloudflareR2SnippetsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDeletedCount);

        // Act
        var result = await _function.RunAsync(It.IsAny<TimerInfo>());

        // Assert
        Assert.That(result, Is.EqualTo(expectedDeletedCount));
        _mediator.Verify(m => m.Send(It.IsAny<CleanupCloudflareR2SnippetsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
} 