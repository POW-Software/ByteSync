using ByteSync.ServerCommon.Business.Announcements;
using ByteSync.ServerCommon.Commands.Announcements;
using ByteSync.ServerCommon.Interfaces.Repositories;
using FakeItEasy;
using FluentAssertions;

namespace ByteSync.ServerCommon.Tests.Commands.Announcements;

[TestFixture]
public class GetActiveAnnouncementsCommandHandlerTests
{
    private IAnnouncementRepository _repository = null!;
    private GetActiveAnnouncementsCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _repository = A.Fake<IAnnouncementRepository>();
        _handler = new GetActiveAnnouncementsCommandHandler(_repository);
    }

    [Test]
    public async Task Handle_ReturnsOnlyActiveAnnouncements()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var announcements = new List<Announcement>
        {
            new() { Id = "1", StartDate = now.AddHours(-1), EndDate = now.AddHours(1) },
            new() { Id = "2", StartDate = now.AddHours(-2), EndDate = now.AddHours(-1) },
            new() { Id = "3", StartDate = now.AddHours(1), EndDate = now.AddHours(2) }
        };
        A.CallTo(() => _repository.GetAll()).Returns(announcements);

        // Act
        var result = await _handler.Handle(new GetActiveAnnouncementsRequest(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("1");
        A.CallTo(() => _repository.GetAll()).MustHaveHappenedOnceExactly();
    }
}
