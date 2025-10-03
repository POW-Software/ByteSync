using System.Reactive.Subjects;
using ByteSync.Business.DataNodes;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Communications.PushReceivers;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.PushReceivers;

[TestFixture]
public class DataNodePushReceiverTests
{
    private Subject<DataNodeDTO> _addedSubject = null!;
    private Subject<DataNodeDTO> _removedSubject = null!;
    private Mock<ISessionService> _sessionServiceMock = null!;
    private Mock<IDataEncrypter> _dataEncrypterMock = null!;
    private Mock<IHubPushHandler2> _hubPushHandlerMock = null!;
    private Mock<IDataNodeService> _dataNodeServiceMock = null!;
    
    [SetUp]
    public void SetUp()
    {
        _addedSubject = new Subject<DataNodeDTO>();
        _removedSubject = new Subject<DataNodeDTO>();
        
        _sessionServiceMock = new Mock<ISessionService>();
        _sessionServiceMock.Setup(s => s.CheckSession(It.IsAny<string>())).Returns(true);
        _dataEncrypterMock = new Mock<IDataEncrypter>();
        _hubPushHandlerMock = new Mock<IHubPushHandler2>();
        _dataNodeServiceMock = new Mock<IDataNodeService>();
        
        _hubPushHandlerMock.SetupGet(h => h.DataNodeAdded).Returns(_addedSubject);
        _hubPushHandlerMock.SetupGet(h => h.DataNodeRemoved).Returns(_removedSubject);
        
        _ = new DataNodePushReceiver(_sessionServiceMock.Object, _dataEncrypterMock.Object,
            _hubPushHandlerMock.Object, _dataNodeServiceMock.Object);
    }
    
    [Test]
    public void DataNodeAdded_ShouldRefreshCodes()
    {
        var encrypted = new EncryptedDataNode();
        var dto = new DataNodeDTO("SID", "CID", encrypted);
        var node = new DataNode();
        _dataEncrypterMock.Setup(e => e.DecryptDataNode(encrypted)).Returns(node);
        
        _addedSubject.OnNext(dto);
        
        _dataNodeServiceMock.Verify(
            s => s.ApplyAddDataNodeLocally(It.Is<DataNode>(d => d == node && d.ClientInstanceId == dto.ClientInstanceId)), Times.Once);
    }
    
    [Test]
    public void DataNodeRemoved_ShouldRefreshCodes()
    {
        var encrypted = new EncryptedDataNode();
        var dto = new DataNodeDTO("SID", "CID", encrypted);
        var node = new DataNode();
        _dataEncrypterMock.Setup(e => e.DecryptDataNode(encrypted)).Returns(node);
        
        _removedSubject.OnNext(dto);
        
        _dataNodeServiceMock.Verify(
            s => s.ApplyRemoveDataNodeLocally(It.Is<DataNode>(d => d == node && d.ClientInstanceId == dto.ClientInstanceId)), Times.Once);
    }
}