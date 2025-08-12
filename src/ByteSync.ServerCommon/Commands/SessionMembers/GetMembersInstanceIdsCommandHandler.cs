using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;

namespace ByteSync.ServerCommon.Commands.SessionMembers;

public class GetMembersInstanceIdsCommandHandler : IRequestHandler<GetMembersInstanceIdsRequest, List<string>>
{
    private readonly ICloudSessionsService _cloudSessionsService;
    
    public GetMembersInstanceIdsCommandHandler(ICloudSessionsService cloudSessionsService)
    {
        _cloudSessionsService = cloudSessionsService;
    }
    
    public async Task<List<string>> Handle(GetMembersInstanceIdsRequest request, CancellationToken cancellationToken)
    {
        return await _cloudSessionsService.GetMembersInstanceIds(request.SessionId);
    }
} 