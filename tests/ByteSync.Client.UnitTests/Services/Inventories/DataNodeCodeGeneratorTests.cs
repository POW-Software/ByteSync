using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
using ByteSync.Client.UnitTests.TestUtilities.Helpers;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Repositories;
using ByteSync.Services.Inventories;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Inventories;

[TestFixture]
public class DataNodeCodeGeneratorTests
{
    private DataNodeRepository _dataNodeRepository = null!;
    private SessionMemberRepository _sessionMemberRepository = null!;
    private DataNodeCodeGenerator _generator = null!;
    
    private Mock<IEnvironmentService> _envMock = null!;
    private Mock<ISessionInvalidationCachePolicy<DataNode, string>> _nodePolicyMock = null!;
    private Mock<IConnectionService> _connMock = null!;
    private Mock<ISessionInvalidationCachePolicy<SessionMember, string>> _memberPolicyMock = null!;
    
    [SetUp]
    public void SetUp()
    {
        _envMock = new Mock<IEnvironmentService>();
        _envMock.SetupGet(e => e.ClientInstanceId).Returns("CID0");
        _nodePolicyMock = new Mock<ISessionInvalidationCachePolicy<DataNode, string>>();
        
        _connMock = new Mock<IConnectionService>();
        _connMock.SetupGet(c => c.ClientInstanceId).Returns("CID0");
        _memberPolicyMock = new Mock<ISessionInvalidationCachePolicy<SessionMember, string>>();
        
        _dataNodeRepository = new DataNodeRepository(_envMock.Object, _nodePolicyMock.Object);
        _sessionMemberRepository = new SessionMemberRepository(_connMock.Object, _memberPolicyMock.Object);
        
        _generator = new DataNodeCodeGenerator(_dataNodeRepository, _sessionMemberRepository);
    }
    
    [TearDown]
    public void TearDown()
    {
        _generator.Dispose();
    }
    
    [Test]
    public void Codes_AreLetters_WhenSingleNodePerMember()
    {
        var member1 = new SessionMember
            { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID0"), JoinedSessionOn = DateTimeOffset.Now.AddSeconds(-10) };
        var member2 = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1"), JoinedSessionOn = DateTimeOffset.Now };
        _sessionMemberRepository.AddOrUpdate([member1, member2]);
        
        var node1 = new DataNode { Id = "N1", ClientInstanceId = "CID0" };
        var node2 = new DataNode { Id = "N2", ClientInstanceId = "CID1" };
        _dataNodeRepository.AddOrUpdate([node1, node2]);
        
        node1.Code.Should().Be("A");
        node2.Code.Should().Be("B");
    }
    
    [Test]
    public void Codes_Update_OnNodeAddAndRemove()
    {
        var member1 = new SessionMember
            { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID0"), JoinedSessionOn = DateTimeOffset.Now.AddSeconds(-10) };
        var member2 = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1"), JoinedSessionOn = DateTimeOffset.Now };
        _sessionMemberRepository.AddOrUpdate([member1, member2]);
        
        var node1 = new DataNode { Id = "N1", ClientInstanceId = "CID0" };
        var node2 = new DataNode { Id = "N2", ClientInstanceId = "CID1" };
        _dataNodeRepository.AddOrUpdate([node1, node2]);
        
        var extra = new DataNode { Id = "N3", ClientInstanceId = "CID0" };
        _dataNodeRepository.AddOrUpdate(extra);
        
        node1.Code.Should().Be("Aa");
        extra.Code.Should().Be("Ab");
        node2.Code.Should().Be("Ba");
        
        _dataNodeRepository.Remove(extra);
        
        node1.Code.Should().Be("A");
        node2.Code.Should().Be("B");
    }
    
    [Test]
    public void RecomputeCodes_DoesNothing_WhenNoNodes()
    {
        var member = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID0"), JoinedSessionOn = DateTimeOffset.Now };
        _sessionMemberRepository.AddOrUpdate(member);
        
        // Should not throw and should not update anything
        _generator.RecomputeCodes();
        
        _dataNodeRepository.Elements.Should().BeEmpty();
    }
    
    [Test]
    public void RecomputeCodes_DoesNothing_WhenNoSessionMembers()
    {
        var node = new DataNode { Id = "N1", ClientInstanceId = "CID0" };
        _dataNodeRepository.AddOrUpdate(node);
        
        // Should not throw and should not update anything
        _generator.RecomputeCodes();
        
        node.Code.Should().BeNullOrEmpty();
    }
    
    [Test]
    public void Codes_UseLowercaseLetters_WhenMultipleNodesPerMember()
    {
        var member1 = new SessionMember
            { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID0"), JoinedSessionOn = DateTimeOffset.Now.AddSeconds(-10) };
        var member2 = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1"), JoinedSessionOn = DateTimeOffset.Now };
        _sessionMemberRepository.AddOrUpdate([member1, member2]);
        
        var node1 = new DataNode { Id = "N1", ClientInstanceId = "CID0" };
        var node2 = new DataNode { Id = "N2", ClientInstanceId = "CID0" };
        var node3 = new DataNode { Id = "N3", ClientInstanceId = "CID0" };
        var node4 = new DataNode { Id = "N4", ClientInstanceId = "CID1" };
        _dataNodeRepository.AddOrUpdate([node1, node2, node3, node4]);
        
        node1.Code.Should().Be("Aa");
        node2.Code.Should().Be("Ab");
        node3.Code.Should().Be("Ac");
        node4.Code.Should().Be("Ba");
    }
    
    [Test]
    public void OrderIndex_IsAssignedSequentially()
    {
        var member1 = new SessionMember
            { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID0"), JoinedSessionOn = DateTimeOffset.Now.AddSeconds(-10) };
        var member2 = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1"), JoinedSessionOn = DateTimeOffset.Now };
        _sessionMemberRepository.AddOrUpdate([member1, member2]);
        
        var node1 = new DataNode { Id = "N1", ClientInstanceId = "CID0" };
        var node2 = new DataNode { Id = "N2", ClientInstanceId = "CID1" };
        var node3 = new DataNode { Id = "N3", ClientInstanceId = "CID0" };
        _dataNodeRepository.AddOrUpdate([node1, node2, node3]);
        
        // Since member1 (CID0) has 2 nodes, singlePerMember becomes false
        // Order is: Member1.N1 (0), Member1.N3 (1), Member2.N2 (2)
        node1.OrderIndex.Should().Be(0);
        node3.OrderIndex.Should().Be(1); // N3 comes after N1 alphabetically within CID0
        node2.OrderIndex.Should().Be(2); // Member2 comes after Member1
    }
    
    [Test]
    public void RecomputeCodes_DoesNotUpdate_WhenCodesAreAlreadyCorrect()
    {
        var member = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID0"), JoinedSessionOn = DateTimeOffset.Now };
        _sessionMemberRepository.AddOrUpdate(member);
        
        var node = new DataNode { Id = "N1", ClientInstanceId = "CID0", Code = "A", OrderIndex = 0 };
        _dataNodeRepository.AddOrUpdate(node);
        
        // First call should not update since codes are already correct
        _generator.RecomputeCodes();
        
        node.Code.Should().Be("A");
        node.OrderIndex.Should().Be(0);
    }
    
    [Test]
    public void RecomputeCodes_HandlesMembersWithoutNodes()
    {
        var member1 = new SessionMember
            { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID0"), JoinedSessionOn = DateTimeOffset.Now.AddSeconds(-10) };
        var member2 = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1"), JoinedSessionOn = DateTimeOffset.Now };
        var member3 = new SessionMember
            { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID2"), JoinedSessionOn = DateTimeOffset.Now.AddSeconds(10) };
        _sessionMemberRepository.AddOrUpdate([member1, member2, member3]);
        
        var node = new DataNode { Id = "N1", ClientInstanceId = "CID1" };
        _dataNodeRepository.AddOrUpdate(node);
        
        // Member1 (CID0) and Member3 (CID2) have no nodes, only Member2 (CID1) has a node
        node.Code.Should().Be("B"); // B because member2 is at index 1
        node.OrderIndex.Should().Be(0);
    }
    
    [Test]
    public void RecomputeCodes_UpdatesOnlyChangedNodes()
    {
        var member1 = new SessionMember
            { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID0"), JoinedSessionOn = DateTimeOffset.Now.AddSeconds(-10) };
        var member2 = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1"), JoinedSessionOn = DateTimeOffset.Now };
        _sessionMemberRepository.AddOrUpdate([member1, member2]);
        
        var node1 = new DataNode { Id = "N1", ClientInstanceId = "CID0", Code = "A", OrderIndex = 0 }; // Already correct
        var node2 = new DataNode { Id = "N2", ClientInstanceId = "CID1", Code = "Wrong", OrderIndex = 999 }; // Needs update
        _dataNodeRepository.AddOrUpdate([node1, node2]);
        
        _generator.RecomputeCodes();
        
        node1.Code.Should().Be("A"); // Should remain unchanged
        node1.OrderIndex.Should().Be(0); // Should remain unchanged
        node2.Code.Should().Be("B"); // Should be updated
        node2.OrderIndex.Should().Be(1); // Should be updated
    }
    
    [Test]
    public void Nodes_AreOrderedByNodeId_WithinSameMember()
    {
        var member = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID0"), JoinedSessionOn = DateTimeOffset.Now };
        _sessionMemberRepository.AddOrUpdate(member);
        
        var node2 = new DataNode { Id = "N2", ClientInstanceId = "CID0" };
        var node1 = new DataNode { Id = "N1", ClientInstanceId = "CID0" };
        var node3 = new DataNode { Id = "N3", ClientInstanceId = "CID0" };
        _dataNodeRepository.AddOrUpdate([node2, node1, node3]);
        
        node1.Code.Should().Be("A"); // N1 comes first alphabetically
        node2.Code.Should().Be("B"); // N2 comes second
        node3.Code.Should().Be("C"); // N3 comes third
    }
    
    [Test]
    public void Dispose_ShouldDisposeSubscription()
    {
        // This test verifies that Dispose doesn't throw
        // The actual subscription disposal is internal to DynamicData
        var disposeAction = () => _generator.Dispose();
        disposeAction.Should().NotThrow();
    }
    
    [Test]
    public void Codes_UseUppercaseLetters_WhenAllMembersHaveSingleNode()
    {
        var member1 = new SessionMember
            { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID0"), JoinedSessionOn = DateTimeOffset.Now.AddSeconds(-10) };
        var member2 = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1"), JoinedSessionOn = DateTimeOffset.Now };
        var member3 = new SessionMember
            { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID2"), JoinedSessionOn = DateTimeOffset.Now.AddSeconds(10) };
        _sessionMemberRepository.AddOrUpdate([member1, member2, member3]);
        
        var node1 = new DataNode { Id = "N1", ClientInstanceId = "CID0" };
        var node2 = new DataNode { Id = "N2", ClientInstanceId = "CID1" };
        var node3 = new DataNode { Id = "N3", ClientInstanceId = "CID2" };
        _dataNodeRepository.AddOrUpdate([node1, node2, node3]);
        
        // Since all members have exactly one node, singlePerMember is true
        // So all nodes get simple uppercase letters
        node1.Code.Should().Be("A");
        node2.Code.Should().Be("B");
        node3.Code.Should().Be("C");
        
        // OrderIndex should be sequential
        node1.OrderIndex.Should().Be(0);
        node2.OrderIndex.Should().Be(1);
        node3.OrderIndex.Should().Be(2);
    }
    
    [Test]
    public void RecomputeCodes_HandlesMixedSingleAndMultipleNodesPerMember()
    {
        var member1 = new SessionMember
            { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID0"), JoinedSessionOn = DateTimeOffset.Now.AddSeconds(-10) };
        var member2 = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1"), JoinedSessionOn = DateTimeOffset.Now };
        var member3 = new SessionMember
            { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID2"), JoinedSessionOn = DateTimeOffset.Now.AddSeconds(10) };
        _sessionMemberRepository.AddOrUpdate([member1, member2, member3]);
        
        var node1 = new DataNode { Id = "N1", ClientInstanceId = "CID0" }; // Single node for member1
        var node2 = new DataNode { Id = "N2", ClientInstanceId = "CID1" }; // Single node for member2
        var node3 = new DataNode { Id = "N3", ClientInstanceId = "CID2" }; // First node for member3
        var node4 = new DataNode { Id = "N4", ClientInstanceId = "CID2" }; // Second node for member3
        _dataNodeRepository.AddOrUpdate([node1, node2, node3, node4]);
        
        // Since member3 has multiple nodes, singlePerMember becomes false
        // So ALL nodes get lowercase letters, even those from members with single nodes
        node1.Code.Should().Be("Aa"); // Member1 gets "A" + lowercase "a"
        node2.Code.Should().Be("Ba"); // Member2 gets "B" + lowercase "a"
        node3.Code.Should().Be("Ca"); // Member3 gets "C" + lowercase "a"
        node4.Code.Should().Be("Cb"); // Member3 gets "C" + lowercase "b"
    }
}