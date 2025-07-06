using ByteSync.ServerCommon.Business.Announcements;
using ByteSync.ServerCommon.Interfaces.Repositories;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Announcements;

public class GetActiveAnnouncementsCommandHandler : IRequestHandler<GetActiveAnnouncementsRequest, List<Announcement>>
{
    private readonly IAnnouncementRepository _repository;

    public GetActiveAnnouncementsCommandHandler(IAnnouncementRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Announcement>> Handle(GetActiveAnnouncementsRequest request, CancellationToken cancellationToken)
    {
        var allAnnouncements = await _repository.GetAll();
        if (allAnnouncements is null)
        {
            return new List<Announcement>();
        }

        var now = DateTime.UtcNow;
        return allAnnouncements.Where(m => m.StartDate <= now && now < m.EndDate).ToList();
    }
}
