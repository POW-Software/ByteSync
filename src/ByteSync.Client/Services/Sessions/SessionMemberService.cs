using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Services.Sessions;

public class SessionMemberService : ISessionMemberService
{
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly ISessionService _sessionService;
    private readonly ISessionMemberApiClient _sessionMemberApiClient;
    private readonly ISessionMemberMapper _sessionMemberMapper;

    public SessionMemberService(ISessionMemberRepository sessionMemberRepository, ISessionService sessionService, 
        ISessionMemberApiClient sessionMemberApiClient, ISessionMemberMapper sessionMemberMapper)
    {
        _sessionMemberRepository = sessionMemberRepository;
        _sessionService = sessionService;
        _sessionMemberApiClient = sessionMemberApiClient;
        _sessionMemberMapper = sessionMemberMapper;
    }
    
    public async Task UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus sessionMemberGeneralStatus)
    {
        var currentSessionMember = _sessionMemberRepository.GetCurrentSessionMember();
        currentSessionMember.SessionMemberGeneralStatus = sessionMemberGeneralStatus;
        
        _sessionMemberRepository.AddOrUpdate(currentSessionMember);

        if (_sessionService.CurrentSession is CloudSession cloudSession)
        {
            var localInventoryStatusParameters = new UpdateSessionMemberGeneralStatusParameters(cloudSession.SessionId, 
                currentSessionMember.ClientInstanceId, sessionMemberGeneralStatus, DateTimeOffset.Now);
                
            await _sessionMemberApiClient.UpdateSessionMemberGeneralStatus(localInventoryStatusParameters)
                .ConfigureAwait(false);
        }
    }

    public void AddOrUpdate(List<SessionMemberInfoDTO> sessionMemberInfoDtos)
    {
        var sessionMemberInfos = new List<SessionMemberInfo>();
        
        foreach (var sessionMemberInfoDto in sessionMemberInfoDtos)
        {
            var sessionMemberInfo = _sessionMemberMapper.Map(sessionMemberInfoDto);
            sessionMemberInfos.Add(sessionMemberInfo);
        }
        
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfos);
    }
}