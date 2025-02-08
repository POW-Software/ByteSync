using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Repositories;
using System.Reactive.Linq;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.PushReceivers;

public class SessionMemberPushReceiver : IPushReceiver
{
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly IHubPushHandler2 _hubPushHandler2;
    private readonly ISessionService _sessionService;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<SessionMemberPushReceiver> _logger;
    private readonly ISessionMemberMapper _sessionMemberMapper;

    public SessionMemberPushReceiver(IHubPushHandler2 hubPushHandler2, ISessionMemberRepository sessionMemberRepository, 
        ISessionService sessionService, IInventoryService inventoryService, ILogger<SessionMemberPushReceiver> logger, 
        ISessionMemberMapper sessionMemberMapper)
    {
        _hubPushHandler2 = hubPushHandler2;
        _sessionMemberRepository = sessionMemberRepository;
        _sessionService = sessionService;
        _inventoryService = inventoryService;
        _logger = logger;
        _sessionMemberMapper = sessionMemberMapper;
        
        _hubPushHandler2.MemberJoinedSession
            .Where(smi => _sessionService.CheckSession(smi.SessionId))
            .Subscribe(smi =>
            {
                var sessionMember = _sessionMemberMapper.Map(smi);
                _sessionMemberRepository.AddOrUpdate(sessionMember);
            });
        
        _hubPushHandler2.MemberQuittedSession
            .Where(csr => _sessionService.CheckSession(csr.SessionId))
            .Subscribe(smi =>
            {
                var elementsToUpdate = new List<SessionMemberInfo>();
                
                foreach (var anySessionMemberInfo in _sessionMemberRepository.Elements)
                {
                    if (anySessionMemberInfo.JoinedSessionOn > smi.JoinedSessionOn)
                    {
                        anySessionMemberInfo.PositionInList -= 1;
                        elementsToUpdate.Add(anySessionMemberInfo);
                    }
                }
                
                _sessionMemberRepository.AddOrUpdate(elementsToUpdate);
                _sessionMemberRepository.Remove(smi);
            });

        _hubPushHandler2.SessionMemberGeneralStatusUpdated
            .Where(s => _sessionService.CheckSession(s.SessionId))
            .Subscribe(status =>
            {
                var sessionMember = _sessionMemberRepository.GetElement(status.ClientInstanceId);
                
                if (sessionMember != null)
                {
                    if (sessionMember.LastLocalInventoryGlobalStatusUpdate == null || 
                        status.UtcChangeDate > sessionMember.LastLocalInventoryGlobalStatusUpdate)
                    {
                        sessionMember.SessionMemberGeneralStatus = status.SessionMemberGeneralStatus;
                        sessionMember.LastLocalInventoryGlobalStatusUpdate = status.UtcChangeDate;

                        if (sessionMember.SessionMemberGeneralStatus.In(SessionMemberGeneralStatus.InventoryCancelled, SessionMemberGeneralStatus.InventoryError))
                        {
                            _logger.LogWarning("Local Inventory is cancelled due to a premature end to another Session Member");
                            _inventoryService.InventoryProcessData.RequestInventoryAbort();
                        }
                        
                        _sessionMemberRepository.AddOrUpdate(sessionMember);
                    }
                }
                else
                {
                    _logger.LogWarning("SessionMemberPushReceiver: SessionMemberGeneralStatusUpdated: session member not found: {@s}", status);
                }
            });
    }
}