using ByteSync.ServerCommon.Business.Announcements;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Announcements;

public class GetActiveAnnouncementsRequest : IRequest<List<Announcement>>;
