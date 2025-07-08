using System.Collections.ObjectModel;
using System.Reactive.Linq;
using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using DynamicData;
using DynamicData.Binding;

namespace ByteSync.Repositories;

public class SessionMemberRepository : BaseSourceCacheRepository<SessionMember, string>, ISessionMemberRepository
{
    private readonly IConnectionService _connectionService;
    private readonly ReadOnlyObservableCollection<SessionMember> _sortedSessionMembersList;
    private readonly ReadOnlyObservableCollection<SessionMember> _sortedOtherSessionMembersList;
    private readonly ISessionInvalidationCachePolicy<SessionMember, string> _sessionInvalidationCachePolicy;

    public SessionMemberRepository(IConnectionService connectionService, 
        ISessionInvalidationCachePolicy<SessionMember, string> sessionInvalidationCachePolicy)
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

    protected override string KeySelector(SessionMember sessionMember) => sessionMember.ClientInstanceId;
    
    public IEnumerable<SessionMember> SortedSessionMembers => _sortedSessionMembersList;

    public IEnumerable<SessionMember> SortedOtherSessionMembers => _sortedOtherSessionMembersList;
    
    public SessionMember GetCurrentSessionMember()
    {
        return GetElement(_connectionService.ClientInstanceId!)!;
    }

    public void Remove(SessionMemberInfoDTO sessionMemberInfoDto)
    {
        Remove(sessionMemberInfoDto.ClientInstanceId);
    }

    public IObservable<ISortedChangeSet<SessionMember, string>> SortedSessionMembersObservable
    {
        get
        {
            return SourceCache.Connect()
                .Sort(SortExpressionComparer<SessionMember>.Ascending(smi => smi.JoinedSessionOn),
                    SortOptimisations.ComparesImmutableValuesOnly);
        }
    }
    
    public IObservable<ISortedChangeSet<SessionMember, string>> SortedOtherSessionMembersObservable
    {
        get
        {
            return SourceCache.Connect()
                .Filter(smi => smi.ClientInstanceId != _connectionService.ClientInstanceId!)
                .Sort(SortExpressionComparer<SessionMember>.Ascending(smi => smi.JoinedSessionOn),
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