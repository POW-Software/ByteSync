using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Sessions.Cloud;
using DynamicData;

namespace ByteSync.Interfaces.Repositories;

public interface ISessionMemberRepository : IBaseSourceCacheRepository<SessionMemberInfo, string>
{
    IObservable<ISortedChangeSet<SessionMemberInfo, string>> SortedSessionMembersObservable { get; }

    IObservable<ISortedChangeSet<SessionMemberInfo, string>> SortedOtherSessionMembersObservable { get; }
    
    IEnumerable<SessionMemberInfo> SortedSessionMembers { get;  }
    
    IEnumerable<SessionMemberInfo> SortedOtherSessionMembers { get;  }
    
    IObservable<bool> IsCurrentUserFirstSessionMemberObservable { get;  }
    
    bool IsCurrentUserFirstSessionMemberCurrentValue { get; }
    
    SessionMemberInfo GetCurrentSessionMember();
    
    void Remove(SessionMemberInfoDTO sessionMemberInfoDto);
}