using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Repositories;
using ByteSync.Services.Inventories;
using ByteSync.Tests.TestUtilities.Helpers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Inventories;

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

    [Test]
    public void Codes_AreLetters_WhenSingleNodePerMember()
    {
        var member1 = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID0"), JoinedSessionOn = DateTimeOffset.Now.AddSeconds(-10) };
        var member2 = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1"), JoinedSessionOn = DateTimeOffset.Now };
        _sessionMemberRepository.AddOrUpdate(new[] { member1, member2 });

        var node1 = new DataNode { NodeId = "N1", ClientInstanceId = "CID0" };
        var node2 = new DataNode { NodeId = "N2", ClientInstanceId = "CID1" };
        _dataNodeRepository.AddOrUpdate(new[] { node1, node2 });

        node1.Code.Should().Be("A");
        node2.Code.Should().Be("B");
    }

    [Test]
    public void Codes_Update_OnNodeAddAndRemove()
    {
        var member1 = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID0"), JoinedSessionOn = DateTimeOffset.Now.AddSeconds(-10) };
        var member2 = new SessionMember { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1"), JoinedSessionOn = DateTimeOffset.Now };
        _sessionMemberRepository.AddOrUpdate(new[] { member1, member2 });

        var node1 = new DataNode { NodeId = "N1", ClientInstanceId = "CID0" };
        var node2 = new DataNode { NodeId = "N2", ClientInstanceId = "CID1" };
        _dataNodeRepository.AddOrUpdate(new[] { node1, node2 });

        var extra = new DataNode { NodeId = "N3", ClientInstanceId = "CID0" };
        _dataNodeRepository.AddOrUpdate(extra);

        node1.Code.Should().Be("Aa");
        extra.Code.Should().Be("Ab");
        node2.Code.Should().Be("Ba");

        _dataNodeRepository.Remove(extra);

        node1.Code.Should().Be("A");
        node2.Code.Should().Be("B");
    }
}
