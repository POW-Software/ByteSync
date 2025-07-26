using ByteSync.Business.DataNodes;
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
        
        // Use simple letters (A, B, C) if there's only one session member OR if all members have exactly one node
        bool useSimpleLetters = sessionMembers.Count == 1 || nodesByMember.Values.All(list => list.Count == 1);

        var updates = new List<DataNode>();

        int globalIndex = 0;

        if (useSimpleLetters)
        {
            if (sessionMembers.Count == 1)
            {
                // Single member: nodes get A, B, C... sequentially
                for (int mIndex = 0; mIndex < sessionMembers.Count; mIndex++)
                {
                    var member = sessionMembers[mIndex];
                    if (!nodesByMember.TryGetValue(member.ClientInstanceId, out var nodes))
                    {
                        continue;
                    }

                    foreach (var node in nodes)
                    {
                        var code = ((char)('A' + globalIndex)).ToString();
                        var needsUpdate = false;
                        
                        if (node.Code != code)
                        {
                            node.Code = code;
                            needsUpdate = true;
                        }
                        
                        if (node.OrderIndex != globalIndex)
                        {
                            node.OrderIndex = globalIndex;
                            needsUpdate = true;
                        }
                        
                        if (needsUpdate)
                        {
                            updates.Add(node);
                        }
                        
                        globalIndex++;
                    }
                }
            }
            else
            {
                // Multiple members with one node each: each member gets their letter
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
                        var needsUpdate = false;
                        
                        if (node.Code != memberLetter)
                        {
                            node.Code = memberLetter;
                            needsUpdate = true;
                        }
                        
                        if (node.OrderIndex != globalIndex)
                        {
                            node.OrderIndex = globalIndex;
                            needsUpdate = true;
                        }
                        
                        if (needsUpdate)
                        {
                            updates.Add(node);
                        }
                        
                        globalIndex++;
                    }
                }
            }
        }
        else
        {
            // For double letters, use member letter + node index within member
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
                    var needsUpdate = false;
                    
                    if (nodes[i].Code != code)
                    {
                        nodes[i].Code = code;
                        needsUpdate = true;
                    }
                    
                    if (nodes[i].OrderIndex != globalIndex)
                    {
                        nodes[i].OrderIndex = globalIndex;
                        needsUpdate = true;
                    }
                    
                    if (needsUpdate)
                    {
                        updates.Add(nodes[i]);
                    }
                    
                    globalIndex++;
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
