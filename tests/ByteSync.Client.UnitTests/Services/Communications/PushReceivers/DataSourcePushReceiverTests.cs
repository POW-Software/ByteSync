using System.Reactive.Subjects;
using ByteSync.Business.DataSources;
using ByteSync.Common.Business.Inventories;
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
public class DataSourcePushReceiverTests
{
    private Subject<DataSourceDTO> _addedSubject = null!;
    private Subject<DataSourceDTO> _removedSubject = null!;
    private Mock<ISessionService> _sessionServiceMock = null!;
    private Mock<IDataEncrypter> _dataEncrypterMock = null!;
    private Mock<IHubPushHandler2> _hubPushHandlerMock = null!;
    private Mock<IDataSourceService> _dataSourceServiceMock = null!;
    
    [SetUp]
    public void SetUp()
    {
        _addedSubject = new Subject<DataSourceDTO>();
        _removedSubject = new Subject<DataSourceDTO>();
        
        _sessionServiceMock = new Mock<ISessionService>();
        _sessionServiceMock.Setup(s => s.CheckSession(It.IsAny<string>())).Returns(true);
        _dataEncrypterMock = new Mock<IDataEncrypter>();
        _hubPushHandlerMock = new Mock<IHubPushHandler2>();
        _dataSourceServiceMock = new Mock<IDataSourceService>();
        
        _hubPushHandlerMock.SetupGet(h => h.DataSourceAdded).Returns(_addedSubject);
        _hubPushHandlerMock.SetupGet(h => h.DataSourceRemoved).Returns(_removedSubject);
        
        _ = new DataSourcePushReceiver(_sessionServiceMock.Object, _dataEncrypterMock.Object,
            _hubPushHandlerMock.Object, _dataSourceServiceMock.Object);
    }
    
    [Test]
    public void DataSourceAdded_ShouldRefreshCodes()
    {
        var encrypted = new EncryptedDataSource();
        var dto = new DataSourceDTO("SID", "CID", encrypted);
        var source = new DataSource { DataNodeId = "NODE" };
        _dataEncrypterMock.Setup(e => e.DecryptDataSource(encrypted)).Returns(source);
        
        _addedSubject.OnNext(dto);
        
        _dataSourceServiceMock.Verify(s => s.ApplyAddDataSourceLocally(source), Times.Once);
    }
    
    [Test]
    public void DataSourceRemoved_ShouldRefreshCodes()
    {
        var encrypted = new EncryptedDataSource();
        var dto = new DataSourceDTO("SID", "CID", encrypted);
        var source = new DataSource { DataNodeId = "NODE" };
        _dataEncrypterMock.Setup(e => e.DecryptDataSource(encrypted)).Returns(source);
        
        _removedSubject.OnNext(dto);
        
        _dataSourceServiceMock.Verify(s => s.ApplyRemoveDataSourceLocally(source), Times.Once);
    }
}