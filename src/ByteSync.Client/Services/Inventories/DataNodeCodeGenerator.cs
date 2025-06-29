using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using DynamicData;

namespace ByteSync.Services.Inventories;

public class DataNodeCodeGenerator : IDataNodeCodeGenerator, IDisposable
{
    private readonly IDataNodeRepository _dataNodeRepository;
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly IDisposable _subscription;

    public DataNodeCodeGenerator(IDataNodeRepository dataNodeRepository, ISessionMemberRepository sessionMemberRepository)
    {
        _dataNodeRepository = dataNodeRepository;
        _sessionMemberRepository = sessionMemberRepository;

        _subscription = _dataNodeRepository.ObservableCache.Connect()
            .WhereReasonsAre(ChangeReason.Add, ChangeReason.Remove)
            .Subscribe(_ => RecomputeCodes());
    }

    public void RecomputeCodes()
    {
        var allNodes = _dataNodeRepository.Elements.ToList();
        if (allNodes.Count == 0)
        {
            return;
        }

        var nodesByMember = allNodes
            .GroupBy(n => n.ClientInstanceId)
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n.NodeId).ToList());

        bool singlePerMember = nodesByMember.Values.All(list => list.Count == 1);

        var sessionMembers = _sessionMemberRepository.SortedSessionMembers.ToList();
        var updates = new List<DataNode>();

        for (int mIndex = 0; mIndex < sessionMembers.Count; mIndex++)
        {
            var member = sessionMembers[mIndex];
            if (!nodesByMember.TryGetValue(member.ClientInstanceId, out var nodes))
            {
                continue;
            }

            var memberLetter = ((char)('A' + mIndex)).ToString();

            if (singlePerMember)
            {
                foreach (var node in nodes)
                {
                    if (node.Code != memberLetter)
                    {
                        node.Code = memberLetter;
                        updates.Add(node);
                    }
                }
            }
            else
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    var code = memberLetter + ((char)('a' + i));
                    if (nodes[i].Code != code)
                    {
                        nodes[i].Code = code;
                        updates.Add(nodes[i]);
                    }
                }
            }
        }

        if (updates.Count > 0)
        {
            _dataNodeRepository.AddOrUpdate(updates);
        }
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}
