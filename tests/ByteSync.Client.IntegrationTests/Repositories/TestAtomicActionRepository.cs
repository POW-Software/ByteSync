using Autofac;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Repositories;
using ByteSync.Services.Communications;
using ByteSync.Services.Communications.SignalR;
using ByteSync.Services.Sessions;
using ByteSync.TestsCommon;

namespace ByteSync.Client.IntegrationTests.Repositories;

public class TestAtomicActionRepository : IntegrationTest
{
    private AtomicActionRepository _atomicActionRepository;

    [SetUp]
    public void Setup()
    {
        RegisterType<ConnectionService, IConnectionService>();
        RegisterType<HubPushHandler2, IHubPushHandler2>();
        RegisterType<CloudProxy, ICloudProxy>();
        RegisterType<SessionService, ISessionService>();
        RegisterType<SessionInvalidationCachePolicy<AtomicAction, string>, ISessionInvalidationCachePolicy<AtomicAction, string>>();
        RegisterType<PropertyIndexer<AtomicAction, ComparisonItem>, IPropertyIndexer<AtomicAction, ComparisonItem>>();
        RegisterType<AtomicActionRepository>();
        BuildMoqContainer();

        _testDirectoryService.CreateTestDirectory();
        _atomicActionRepository = Container.Resolve<AtomicActionRepository>();
    }

    [Test]
    public void AddOrUpdate_ShouldAddAtomicAction()
    {
        // Arrange
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/fileToCopy1.txt", "fileToCopy1.txt", "/fileToCopy1.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        var atomicAction = new AtomicAction { AtomicActionId = "Action1", ComparisonItem = comparisonItem };

        // Act
        _atomicActionRepository.AddOrUpdate(atomicAction);
        var result = _atomicActionRepository.GetAtomicActions(comparisonItem);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.First().AtomicActionId, Is.EqualTo("Action1"));
    }

    [Test]
    public void Remove_ShouldRemoveAtomicAction()
    {
        // Arrange
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/fileToCopy2.txt", "fileToCopy2.txt", "/fileToCopy2.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        var atomicAction = new AtomicAction { AtomicActionId = "Action2", ComparisonItem = comparisonItem };
        _atomicActionRepository.AddOrUpdate(atomicAction);

        // Act
        _atomicActionRepository.Remove(atomicAction);
        var result = _atomicActionRepository.GetAtomicActions(comparisonItem);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetAtomicActions_ShouldReturnCorrectActionsForComparisonItem()
    {
        // Arrange
        var pathIdentity1 = new PathIdentity(FileSystemTypes.File, "/fileToCopy1.txt", "fileToCopy1.txt", "/fileToCopy1.txt");
        var comparisonItem1 =  new ComparisonItem(pathIdentity1);
        var pathIdentity2 = new PathIdentity(FileSystemTypes.File, "/fileToCopy2.txt", "fileToCopy2.txt", "/fileToCopy2.txt");
        var comparisonItem2 =  new ComparisonItem(pathIdentity2);

        var atomicAction1 = new AtomicAction { AtomicActionId = "Action3", ComparisonItem = comparisonItem1 };
        var atomicAction2 = new AtomicAction { AtomicActionId = "Action4", ComparisonItem = comparisonItem2 };

        _atomicActionRepository.AddOrUpdate([atomicAction1, atomicAction2]);

        // Act
        var result1 = _atomicActionRepository.GetAtomicActions(comparisonItem1);
        var result2 = _atomicActionRepository.GetAtomicActions(comparisonItem2);

        // Assert
        Assert.That(result1, Has.Count.EqualTo(1));
        Assert.That(result1.First().AtomicActionId, Is.EqualTo("Action3"));

        Assert.That(result2, Has.Count.EqualTo(1));
        Assert.That(result2.First().AtomicActionId, Is.EqualTo("Action4"));
    }
    
    [Test]
    public async Task GetAtomicActions_ShouldReturnEmptyAfterSessionReset()
    {
        // Arrange
        var pathIdentity1 = new PathIdentity(FileSystemTypes.File, "/fileToCopy1.txt", "fileToCopy1.txt", "/fileToCopy1.txt");
        var comparisonItem1 =  new ComparisonItem(pathIdentity1);
        var pathIdentity2 = new PathIdentity(FileSystemTypes.File, "/fileToCopy2.txt", "fileToCopy2.txt", "/fileToCopy2.txt");
        var comparisonItem2 =  new ComparisonItem(pathIdentity2);

        var atomicAction1 = new AtomicAction { AtomicActionId = "Action3", ComparisonItem = comparisonItem1 };
        var atomicAction2 = new AtomicAction { AtomicActionId = "Action4", ComparisonItem = comparisonItem2 };

        _atomicActionRepository.AddOrUpdate([atomicAction1, atomicAction2]);

        // Act
        var sessionService = Container.Resolve<ISessionService>();
        await sessionService.ResetSession();
        
        var result1 = _atomicActionRepository.GetAtomicActions(comparisonItem1);
        var result2 = _atomicActionRepository.GetAtomicActions(comparisonItem2);

        // Assert
        Assert.That(result1, Is.Empty);
        Assert.That(result2, Is.Empty);
    }
}
