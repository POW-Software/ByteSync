using System.Collections.ObjectModel;
using System.Reactive.Linq;
using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using DynamicData;
using DynamicData.Binding;

namespace ByteSync.Repositories;

public class SessionMemberRepository : BaseSourceCacheRepository<SessionMemberInfo, string>, ISessionMemberRepository
{
    private readonly IConnectionService _connectionService;
    private readonly ReadOnlyObservableCollection<SessionMemberInfo> _sortedSessionMembersList;
    private readonly ReadOnlyObservableCollection<SessionMemberInfo> _sortedOtherSessionMembersList;
    private readonly ISessionInvalidationCachePolicy<SessionMemberInfo, string> _sessionInvalidationCachePolicy;

    public SessionMemberRepository(IConnectionService connectionService, 
        ISessionInvalidationCachePolicy<SessionMemberInfo, string> sessionInvalidationCachePolicy)
    {
        _connectionService = connectionService;
        
        SortedSessionMembersObservable
            .Bind(out var sortedSessionMembersList)
            .Subscribe();
        _sortedSessionMembersList = sortedSessionMembersList;
        
        SortedOtherSessionMembersObservable
            .Bind(out var sortedOtherSessionMembersList)
            .Subscribe();
        _sortedOtherSessionMembersList = sortedOtherSessionMembersList;
        
        IsCurrentUserFirstSessionMemberObservable
            .Subscribe(value => IsCurrentUserFirstSessionMemberCurrentValue = value);
        
        _sessionInvalidationCachePolicy = sessionInvalidationCachePolicy;
        _sessionInvalidationCachePolicy.Initialize(SourceCache, true, false);
    }

    protected override string KeySelector(SessionMemberInfo sessionMemberInfo) => sessionMemberInfo.ClientInstanceId;
    
    public IEnumerable<SessionMemberInfo> SortedSessionMembers => _sortedSessionMembersList;

    public IEnumerable<SessionMemberInfo> SortedOtherSessionMembers => _sortedOtherSessionMembersList;
    
    public SessionMemberInfo GetCurrentSessionMember()
    {
        return GetElement(_connectionService.ClientInstanceId!)!;
    }

    public void Remove(SessionMemberInfoDTO sessionMemberInfoDto)
    {
        Remove(sessionMemberInfoDto.ClientInstanceId);
    }

    public IObservable<ISortedChangeSet<SessionMemberInfo, string>> SortedSessionMembersObservable
    {
        get
        {
            return SourceCache.Connect()
                .Sort(SortExpressionComparer<SessionMemberInfo>.Ascending(smi => smi.JoinedSessionOn),
                    SortOptimisations.ComparesImmutableValuesOnly);
        }
    }
    
    public IObservable<ISortedChangeSet<SessionMemberInfo, string>> SortedOtherSessionMembersObservable
    {
        get
        {
            return SourceCache.Connect()
                .Filter(smi => smi.ClientInstanceId != _connectionService.ClientInstanceId!)
                .Sort(SortExpressionComparer<SessionMemberInfo>.Ascending(smi => smi.JoinedSessionOn),
                    SortOptimisations.ComparesImmutableValuesOnly);
        }
    }
    
    public IObservable<bool> IsCurrentUserFirstSessionMemberObservable
    {
        get
        {
            return SortedSessionMembersObservable
                .Select(changes => changes.SortedItems.FirstOrDefault())
                .Select(firstMember => firstMember.Key == _connectionService.ClientInstanceId!)
                .DistinctUntilChanged();
        }
    }

    public bool IsCurrentUserFirstSessionMemberCurrentValue { get; private set; }
}