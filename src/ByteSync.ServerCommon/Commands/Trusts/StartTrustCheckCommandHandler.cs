using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Versions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Trusts;

public class StartTrustCheckCommandHandler : IRequestHandler<StartTrustCheckRequest, StartTrustCheckResult>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<StartTrustCheckCommandHandler> _logger;
    
    public StartTrustCheckCommandHandler(ICloudSessionsRepository cloudSessionsRepository, IInvokeClientsService invokeClientsService,
        ILogger<StartTrustCheckCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }
    
    public async Task<StartTrustCheckResult> Handle(StartTrustCheckRequest request, CancellationToken cancellationToken)
    {
        var trustCheckParameters = request.Parameters;
        var joiner = request.Client;
        
        _logger.LogInformation(
            "[PROTOCOL_VERSION_DEBUG] StartTrustCheck - Received request: JoinerId={JoinerId}, SessionId={SessionId}, ProtocolVersion={ProtocolVersion}, PublicKeyInfo.ProtocolVersion={PublicKeyInfoProtocolVersion}",
            joiner.ClientInstanceId, trustCheckParameters.SessionId, trustCheckParameters.ProtocolVersion,
            trustCheckParameters.PublicKeyInfo.ProtocolVersion);
        
        var cloudSession = await _cloudSessionsRepository.Get(trustCheckParameters.SessionId).ConfigureAwait(false);
        if (cloudSession == null)
        {
            _logger.LogWarning("[PROTOCOL_VERSION_DEBUG] StartTrustCheck - Session not found: SessionId={SessionId}",
                trustCheckParameters.SessionId);
            
            return new StartTrustCheckResult { IsOK = false };
        }
        
        var joinerProtocolVersion = trustCheckParameters.ProtocolVersion;
        
        _logger.LogInformation(
            "[PROTOCOL_VERSION_DEBUG] StartTrustCheck - Checking joiner protocol version: JoinerVersion={JoinerVersion}, IsCompatible={IsCompatible}",
            joinerProtocolVersion, ProtocolVersion.IsCompatible(joinerProtocolVersion));
        
        if (!ProtocolVersion.IsCompatible(joinerProtocolVersion))
        {
            _logger.LogWarning(
                "[PROTOCOL_VERSION_DEBUG] StartTrustCheck: Joiner {JoinerId} has incompatible protocol version {JoinerVersion}",
                joiner.ClientInstanceId, joinerProtocolVersion);
            
            return new StartTrustCheckResult { IsOK = false, IsProtocolVersionIncompatible = true };
        }
        
        foreach (var member in cloudSession.SessionMembers)
        {
            if (trustCheckParameters.MembersInstanceIdsToCheck.Contains(member.ClientInstanceId))
            {
                var memberProtocolVersion = member.PublicKeyInfo.ProtocolVersion;
                
                _logger.LogInformation(
                    "[PROTOCOL_VERSION_DEBUG] StartTrustCheck - Checking member: MemberId={MemberId}, MemberVersion={MemberVersion}, JoinerVersion={JoinerVersion}, IsCompatible={IsCompatible}, VersionsMatch={VersionsMatch}",
                    member.ClientInstanceId, memberProtocolVersion, joinerProtocolVersion,
                    ProtocolVersion.IsCompatible(memberProtocolVersion), memberProtocolVersion == joinerProtocolVersion);
                
                if (!ProtocolVersion.IsCompatible(memberProtocolVersion) ||
                    memberProtocolVersion != joinerProtocolVersion)
                {
                    _logger.LogWarning(
                        "[PROTOCOL_VERSION_DEBUG] StartTrustCheck: Protocol version mismatch between joiner {JoinerId} (version {JoinerVersion}) and member {MemberId} (version {MemberVersion})",
                        joiner.ClientInstanceId, joinerProtocolVersion, member.ClientInstanceId, memberProtocolVersion);
                    
                    return new StartTrustCheckResult { IsOK = false, IsProtocolVersionIncompatible = true };
                }
            }
        }
        
        _logger.LogInformation("StartTrustCheck: {Joiner} starts trust check for session {SessionId}. {Count} members to check",
            joiner.ClientInstanceId, trustCheckParameters.SessionId, trustCheckParameters.MembersInstanceIdsToCheck.Count);
        
        List<string> members = new List<string>();
        foreach (var clientInstanceId in trustCheckParameters.MembersInstanceIdsToCheck)
        {
            if (cloudSession.SessionMembers.Any(sm => sm.ClientInstanceId == clientInstanceId))
            {
                members.Add(clientInstanceId);
                
                _logger.LogInformation("StartTrustCheck: {Member} must be trusted by {Joiner}",
                    clientInstanceId, joiner.ClientInstanceId);
                
                _logger.LogInformation(
                    "[PROTOCOL_VERSION_DEBUG] StartTrustCheck - Sending AskPublicKeyCheckData to member: MemberId={MemberId}, JoinerId={JoinerId}, JoinerPublicKeyInfo.ProtocolVersion={JoinerPublicKeyInfoProtocolVersion}",
                    clientInstanceId, joiner.ClientInstanceId, trustCheckParameters.PublicKeyInfo.ProtocolVersion);
                
                await _invokeClientsService.Client(clientInstanceId).AskPublicKeyCheckData(trustCheckParameters.SessionId,
                    joiner.ClientInstanceId,
                    trustCheckParameters.PublicKeyInfo).ConfigureAwait(false);
            }
        }
        
        return new StartTrustCheckResult { IsOK = true, MembersInstanceIds = members };
    }
}