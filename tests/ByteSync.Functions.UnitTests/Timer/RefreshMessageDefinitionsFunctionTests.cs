using ByteSync.Functions.Timer;
using ByteSync.ServerCommon.Business.Messages;
using ByteSync.ServerCommon.Interfaces.Loaders;
using ByteSync.ServerCommon.Interfaces.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;

namespace ByteSync.Functions.UnitTests.Timer;

[TestFixture]
public class RefreshMessageDefinitionsFunctionTests
{
    private Mock<IMessageDefinitionsLoader> _loader = null!;
    private Mock<IMessageDefinitionRepository> _repository = null!;
    private Mock<ILogger<RefreshMessageDefinitionsFunction>> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _loader = new Mock<IMessageDefinitionsLoader>();
        _repository = new Mock<IMessageDefinitionRepository>();
        _logger = new Mock<ILogger<RefreshMessageDefinitionsFunction>>();
    }

    [Test]
    public async Task RunAsync_ShouldFilterExpiredMessages()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var messages = new List<MessageDefinition>
        {
            new MessageDefinition { StartDate = now.AddHours(-1), EndDate = now.AddHours(1), Message = new Dictionary<string,string>{{"en","valid"}} },
            new MessageDefinition { StartDate = now.AddHours(-2), EndDate = now.AddHours(-1), Message = new Dictionary<string,string>{{"en","expired"}} }
        };
        _loader.Setup(l => l.Load()).ReturnsAsync(messages);
        var function = new RefreshMessageDefinitionsFunction(_loader.Object, _repository.Object, _logger.Object);

        // Act
        var result = await function.RunAsync(new TimerInfo());

        // Assert
        _loader.Verify(l => l.Load(), Times.Once);
        _repository.Verify(r => r.SaveAll(It.Is<List<MessageDefinition>>(l => l.Count == 1 && l[0].Message["en"] == "valid")), Times.Once);
        Assert.That(result, Is.EqualTo(1));
    }
}
