using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Sessions.Cloud;
using DynamicData;

namespace ByteSync.Interfaces.Repositories;

public interface ISessionMemberRepository : IBaseSourceCacheRepository<SessionMember, string>
{
    IObservable<ISortedChangeSet<SessionMember, string>> SortedSessionMembersObservable { get; }

    IObservable<ISortedChangeSet<SessionMember, string>> SortedOtherSessionMembersObservable { get; }
    
    IEnumerable<SessionMember> SortedSessionMembers { get;  }
    
    IEnumerable<SessionMember> SortedOtherSessionMembers { get;  }
    
    IObservable<bool> IsCurrentUserFirstSessionMemberObservable { get;  }
    
    bool IsCurrentUserFirstSessionMemberCurrentValue { get; }
    
    SessionMember GetCurrentSessionMember();
    
    void Remove(SessionMemberInfoDTO sessionMemberInfoDto);
}