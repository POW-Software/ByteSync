using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Repositories;
using ByteSync.Services.Inventories;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Inventories;

[TestFixture]
public class DataSourceCodeGeneratorTests
{
    private DataSourceRepository _dataSourceRepository = null!;
    private DataNodeRepository _dataNodeRepository = null!;
    private DataSourceCodeGenerator _generator = null!;

    private Mock<IEnvironmentService> _envMock = null!;
    private Mock<ISessionInvalidationCachePolicy<DataSource, string>> _dsPolicyMock = null!;
    private Mock<ISessionInvalidationCachePolicy<DataNode, string>> _nodePolicyMock = null!;

    [SetUp]
    public void SetUp()
    {
        _envMock = new Mock<IEnvironmentService>();
        _envMock.SetupGet(e => e.ClientInstanceId).Returns("CID0");
        _dsPolicyMock = new Mock<ISessionInvalidationCachePolicy<DataSource, string>>();
        _nodePolicyMock = new Mock<ISessionInvalidationCachePolicy<DataNode, string>>();

        _dataSourceRepository = new DataSourceRepository(_envMock.Object, _dsPolicyMock.Object);
        _dataNodeRepository = new DataNodeRepository(_envMock.Object, _nodePolicyMock.Object);

        _generator = new DataSourceCodeGenerator(_dataSourceRepository, _dataNodeRepository);
    }

    [TearDown]
    public void TearDown()
    {
        _generator.Dispose();
    }

    [Test]
    public void Codes_Assigned_Sequentially_OnAdd()
    {
        var node = new DataNode { NodeId = "N1", ClientInstanceId = "CID0", Code = "A" };
        _dataNodeRepository.AddOrUpdate(node);

        var ds1 = new DataSource { DataNodeId = node.NodeId, ClientInstanceId = "CID0", Path = "path1", InitialTimestamp = DateTime.Now};
        var ds2 = new DataSource { DataNodeId = node.NodeId, ClientInstanceId = "CID0", Path = "path2", InitialTimestamp = DateTime.Now.AddMilliseconds(1) };

        _dataSourceRepository.AddOrUpdate(ds1);
        _dataSourceRepository.AddOrUpdate(ds2);

        ds1.Code.Should().Be("A1");
        ds2.Code.Should().Be("A2");
    }

    [Test]
    public void Codes_Renumber_OnRemove()
    {
        var node = new DataNode { NodeId = "N1", ClientInstanceId = "CID0", Code = "A" };
        _dataNodeRepository.AddOrUpdate(node);

        var ds1 = new DataSource { DataNodeId = node.NodeId, ClientInstanceId = "CID0", Path = "path1", InitialTimestamp = DateTime.Now };
        var ds2 = new DataSource { DataNodeId = node.NodeId, ClientInstanceId = "CID0", Path = "path2", InitialTimestamp = DateTime.Now.AddMilliseconds(1) };
        var ds3 = new DataSource { DataNodeId = node.NodeId, ClientInstanceId = "CID0", Path = "path3", InitialTimestamp = DateTime.Now.AddMilliseconds(2) };
        _dataSourceRepository.AddOrUpdate(new[] { ds1, ds2, ds3 });

        _dataSourceRepository.Remove(ds2);

        ds1.Code.Should().Be("A1");
        ds3.Code.Should().Be("A2");
    }

    [Test]
    public void Codes_Update_WhenNodeCodeChanges()
    {
        var node = new DataNode { NodeId = "N1", ClientInstanceId = "CID0", Code = "A" };
        _dataNodeRepository.AddOrUpdate(node);

        var ds1 = new DataSource { DataNodeId = node.NodeId, ClientInstanceId = "CID0", Path = "path1", InitialTimestamp = DateTime.Now };
        var ds2 = new DataSource { DataNodeId = node.NodeId, ClientInstanceId = "CID0", Path = "path2", InitialTimestamp = DateTime.Now.AddMilliseconds(1) };
        _dataSourceRepository.AddOrUpdate(new[] { ds1, ds2 });

        node.Code = "B";
        _dataNodeRepository.AddOrUpdate(node);

        ds1.Code.Should().Be("B1");
        ds2.Code.Should().Be("B2");
    }
}
