using ByteSync.ServerCommon.Business.Messages;
using ByteSync.ServerCommon.Commands.Messages;
using ByteSync.ServerCommon.Interfaces.Repositories;
using FakeItEasy;
using FluentAssertions;

namespace ByteSync.ServerCommon.Tests.Commands.Messages;

[TestFixture]
public class GetActiveMessagesCommandHandlerTests
{
    private IMessageDefinitionRepository _repository = null!;
    private GetActiveMessagesCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _repository = A.Fake<IMessageDefinitionRepository>();
        _handler = new GetActiveMessagesCommandHandler(_repository);
    }

    [Test]
    public async Task Handle_ReturnsOnlyActiveMessages()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var messages = new List<MessageDefinition>
        {
            new() { Id = "1", StartDate = now.AddHours(-1), EndDate = now.AddHours(1) },
            new() { Id = "2", StartDate = now.AddHours(-2), EndDate = now.AddHours(-1) },
            new() { Id = "3", StartDate = now.AddHours(1), EndDate = now.AddHours(2) }
        };
        A.CallTo(() => _repository.GetAll()).Returns(messages);

        // Act
        var result = await _handler.Handle(new GetActiveMessagesRequest(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("1");
        A.CallTo(() => _repository.GetAll()).MustHaveHappenedOnceExactly();
    }
}
