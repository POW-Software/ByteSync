using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class GetMembersCommandHandler : IRequestHandler<GetMembersRequest, List<SessionMemberInfoDTO>>
{
    private readonly ICloudSessionsService _cloudSessionsService;
    
    public GetMembersCommandHandler(ICloudSessionsService cloudSessionsService)
    {
        _cloudSessionsService = cloudSessionsService;
    }
    
    public async Task<List<SessionMemberInfoDTO>> Handle(GetMembersRequest request, CancellationToken cancellationToken)
    {
        return await _cloudSessionsService.GetSessionMembersInfosAsync(request.SessionId);
    }
} 