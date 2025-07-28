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

        // Note: This subscription explicitly filters for ChangeReason.Add and ChangeReason.Remove.
        // Repository updates invoked via AddOrUpdate (which trigger ChangeReason.Update) are not handled here,
        // ensuring that RecomputeCodes does not result in an infinite update loop.
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
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n.Id).ToList());

        var sessionMembers = _sessionMemberRepository.SortedSessionMembers.ToList();
        var updates = new List<DataNode>();
        int globalIndex = 0;
        
        bool useSimpleLetters = sessionMembers.Count == 1 || nodesByMember.Values.All(list => list.Count == 1);
        
        if (useSimpleLetters && sessionMembers.Count == 1)
        {
            ProcessSingleMember(sessionMembers, nodesByMember, updates, ref globalIndex);
        }
        else if (useSimpleLetters)
        {
            ProcessMultipleMembersOneNodeEach(sessionMembers, nodesByMember, updates, ref globalIndex);
        }
        else
        {
            ProcessMultipleMembersMultipleNodes(sessionMembers, nodesByMember, updates, ref globalIndex);
        }

        if (updates.Count > 0)
        {
            _dataNodeRepository.AddOrUpdate(updates);
        }
    }
    
    private void ProcessSingleMember(List<SessionMember> sessionMembers, Dictionary<string, List<DataNode>> nodesByMember, List<DataNode> updates, ref int globalIndex)
    {
        var member = sessionMembers[0];
        if (!nodesByMember.TryGetValue(member.ClientInstanceId, out var nodes))
        {
            return;
        }

        foreach (var node in nodes)
        {
            var code = ((char)('A' + globalIndex)).ToString();
            TryUpdateNode(node, code, globalIndex, updates);
            globalIndex++;
        }
    }

    private void ProcessMultipleMembersOneNodeEach(List<SessionMember> sessionMembers, Dictionary<string, List<DataNode>> nodesByMember, List<DataNode> updates, ref int globalIndex)
    {
        for (int mIndex = 0; mIndex < sessionMembers.Count; mIndex++)
        {
            var member = sessionMembers[mIndex];
            if (!nodesByMember.TryGetValue(member.ClientInstanceId, out var nodes))
            {
                continue;
            }

            var memberLetter = ((char)('A' + mIndex)).ToString();

            foreach (var node in nodes)
            {
                TryUpdateNode(node, memberLetter, globalIndex, updates);
                globalIndex++;
            }
        }
    }

    private void ProcessMultipleMembersMultipleNodes(List<SessionMember> sessionMembers, Dictionary<string, List<DataNode>> nodesByMember, List<DataNode> updates, ref int globalIndex)
    {
        for (int mIndex = 0; mIndex < sessionMembers.Count; mIndex++)
        {
            var member = sessionMembers[mIndex];
            if (!nodesByMember.TryGetValue(member.ClientInstanceId, out var nodes))
            {
                continue;
            }

            var memberLetter = ((char)('A' + mIndex)).ToString();

            for (int i = 0; i < nodes.Count; i++)
            {
                var code = memberLetter + ((char)('a' + i));
                TryUpdateNode(nodes[i], code, globalIndex, updates);
                globalIndex++;
            }
        }
    }
    
    private bool TryUpdateNode(DataNode node, string code, int orderIndex, List<DataNode> updates)
    {
        bool needsUpdate = false;

        if (node.Code != code)
        {
            node.Code = code;
            needsUpdate = true;
        }

        if (node.OrderIndex != orderIndex)
        {
            node.OrderIndex = orderIndex;
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            updates.Add(node);
        }

        return needsUpdate;
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}
