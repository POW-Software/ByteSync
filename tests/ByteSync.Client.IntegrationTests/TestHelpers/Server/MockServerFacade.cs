using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Controls.Communications.Http;
using Moq;

namespace ByteSync.Client.IntegrationTests.TestHelpers.Server;

public class MockServerFacade
{
    private readonly Dictionary<string, SessionConfiguration> _sessions = new();
    private readonly Mock<ITrustApiClient> _trustApiClient;
    private readonly Mock<ISessionMemberApiClient> _sessionMemberApiClient;
    
    public MockServerFacade()
    {
        _trustApiClient = new Mock<ITrustApiClient>();
        _sessionMemberApiClient = new Mock<ISessionMemberApiClient>();
    }
    
    public MockServerFacade WithSession(string sessionId, params string[] memberInstanceIds)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            _sessions[sessionId] = new SessionConfiguration
            {
                SessionId = sessionId,
                Members = new List<MemberConfiguration>()
            };
        }
        
        var session = _sessions[sessionId];
        var newMemberIds = memberInstanceIds.Where(id => !session.Members.Any(m => m.InstanceId == id));
        foreach (var memberId in newMemberIds)
        {
            session.Members.Add(new MemberConfiguration
            {
                InstanceId = memberId,
                ProtocolVersion = ProtocolVersion.CURRENT
            });
        }
        
        _sessionMemberApiClient
            .Setup(c => c.GetMembersClientInstanceIds(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Members.Select(m => m.InstanceId).ToList());
        
        return this;
    }
    
    public MockServerFacade WithMemberProtocolVersion(string sessionId, string memberInstanceId, int protocolVersion)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            WithSession(sessionId, memberInstanceId);
        }
        
        var member = _sessions[sessionId].Members.FirstOrDefault(m => m.InstanceId == memberInstanceId);
        if (member != null)
        {
            member.ProtocolVersion = protocolVersion;
        }
        
        return this;
    }
    
    public MockServerFacade WithStartTrustCheckSuccess(string sessionId, params string[] memberInstanceIds)
    {
        _trustApiClient
            .Setup(c => c.StartTrustCheck(
                It.Is<TrustCheckParameters>(p => p.SessionId == sessionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartTrustCheckResult
            {
                IsOK = true,
                IsProtocolVersionIncompatible = false,
                MembersInstanceIds = memberInstanceIds.ToList()
            });
        
        return this;
    }
    
    public MockServerFacade WithStartTrustCheckProtocolVersionIncompatible(string sessionId)
    {
        _trustApiClient
            .Setup(c => c.StartTrustCheck(
                It.Is<TrustCheckParameters>(p => p.SessionId == sessionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartTrustCheckResult
            {
                IsOK = false,
                IsProtocolVersionIncompatible = true,
                MembersInstanceIds = new List<string>()
            });
        
        return this;
    }
    
    public MockServerFacade WithStartTrustCheckProtocolVersionIncompatible(string sessionId, int protocolVersion)
    {
        _trustApiClient
            .Setup(c => c.StartTrustCheck(
                It.Is<TrustCheckParameters>(p => p.SessionId == sessionId && p.ProtocolVersion == protocolVersion),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartTrustCheckResult
            {
                IsOK = false,
                IsProtocolVersionIncompatible = true,
                MembersInstanceIds = new List<string>()
            });
        
        return this;
    }
    
    public MockServerFacade WithStartTrustCheckFailure(string sessionId)
    {
        _trustApiClient
            .Setup(c => c.StartTrustCheck(
                It.Is<TrustCheckParameters>(p => p.SessionId == sessionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartTrustCheckResult
            {
                IsOK = false,
                IsProtocolVersionIncompatible = false,
                MembersInstanceIds = new List<string>()
            });
        
        return this;
    }
    
    public Mock<ITrustApiClient> GetTrustApiClient() => _trustApiClient;
    
    public Mock<ISessionMemberApiClient> GetSessionMemberApiClient() => _sessionMemberApiClient;
    
    public void VerifyStartTrustCheckCalled(string sessionId, Times times)
    {
        _trustApiClient.Verify(
            c => c.StartTrustCheck(
                It.Is<TrustCheckParameters>(p => p.SessionId == sessionId),
                It.IsAny<CancellationToken>()),
            times);
    }
    
    public void VerifyStartTrustCheckCalledOnce(string sessionId)
    {
        VerifyStartTrustCheckCalled(sessionId, Times.Once());
    }
    
    public void VerifyInformProtocolVersionIncompatibleCalled(Times times)
    {
        _trustApiClient.Verify(
            c => c.InformProtocolVersionIncompatible(
                It.IsAny<InformProtocolVersionIncompatibleParameters>(),
                It.IsAny<CancellationToken>()),
            times);
    }
    
    public void VerifyInformProtocolVersionIncompatibleCalledOnce()
    {
        VerifyInformProtocolVersionIncompatibleCalled(Times.Once());
    }
    
    private class SessionConfiguration
    {
        public string SessionId { get; set; } = null!;
        
        public List<MemberConfiguration> Members { get; set; } = new();
    }
    
    private class MemberConfiguration
    {
        public string InstanceId { get; set; } = null!;
        
        public int ProtocolVersion { get; set; }
    }
}