using ByteSync.ServerCommon.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Trusts;

public class SetAuthCheckedCommandHandler: IRequestHandler<SetAuthCheckedRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ILogger<SetAuthCheckedCommandHandler> _logger;

    public SetAuthCheckedCommandHandler(ICloudSessionsRepository cloudSessionsRepository, ILogger<SetAuthCheckedCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _logger = logger;
    }

    public async Task Handle(SetAuthCheckedRequest request, CancellationToken cancellationToken)
    {
        await _cloudSessionsRepository.Update(request.Parameters.SessionId, cloudSessionData =>
        {
            var member = cloudSessionData.FindMemberOrPreMember(request.Client.ClientInstanceId);
            
            if (member == null)
            {
                _logger.LogInformation("{Endpoint} is neither a member nor a premember of session {session}", 
                    request.Client.ClientInstanceId, request.Parameters.SessionId);
                return false;
            }
            
            member.AuthCheckClientInstanceIds.Add(request.Parameters.CheckedClientInstanceId);

            return true;
        });
    }
}