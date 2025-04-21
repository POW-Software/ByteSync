using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Services.Actions;
using ByteSync.TestsCommon;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace ByteSync.Tests.Services.Actions;

[TestFixture]
public class TestSharedActionsGroupComputer : AbstractTester
{
    private SharedActionsGroupComputer _sharedActionsGroupComputer;
    private Mock<ISharedAtomicActionRepository> _sharedAtomicActionRepository;
    private Mock<ISharedActionsGroupRepository> _sharedActionsGroupRepository;
    
    [SetUp]
    public void SetUp()
    {
        _sharedAtomicActionRepository = new Mock<ISharedAtomicActionRepository>();
        _sharedActionsGroupRepository = new Mock<ISharedActionsGroupRepository>();
        
        _sharedActionsGroupComputer = new SharedActionsGroupComputer(_sharedAtomicActionRepository.Object, _sharedActionsGroupRepository.Object);
    }
    
    [Test]
    public async Task Test_Empty()
    {
        await _sharedActionsGroupComputer.ComputeSharedActionsGroups();
        
        _sharedActionsGroupRepository.Verify(s => s.AddOrUpdate(It.IsAny<IEnumerable<SharedActionsGroup>>()), Times.Never);
    }
    
    [Test]
    public async Task Test_1Action_1Source_1Target()
    {
        SharedAtomicAction sharedAtomicAction;

        var dateTime = DateTime.Now.AddMinutes(-10);
        var sharedAtomicActions = new List<SharedAtomicAction>();
        sharedAtomicAction = new SharedAtomicAction("SAA1");
        sharedAtomicAction.PathIdentity = new PathIdentity(FileSystemTypes.File, "file1.txt", "file1.txt", "file1.txt");
        sharedAtomicAction.Source = new SharedDataPart("A1", FileSystemTypes.Directory, "CII_A", "A", "D:\\", "file1.txt", null, null, false);
        sharedAtomicAction.Target = new SharedDataPart("B1", FileSystemTypes.Directory, "CII_B", "B", "D:\\", "file1.txt", null, null, false);
        sharedAtomicAction.Operator = ActionOperatorTypes.SynchronizeContentAndDate;
        sharedAtomicAction.SynchronizationType = SynchronizationTypes.Full;
        sharedAtomicAction.Size = 10;
        sharedAtomicAction.LastWriteTimeUtc = dateTime;
        sharedAtomicActions.Add(sharedAtomicAction);
        
        _sharedAtomicActionRepository.Setup(m => m.Elements).Returns(sharedAtomicActions);
        
        List<IEnumerable<SharedActionsGroup>> capturedGroups = new List<IEnumerable<SharedActionsGroup>>();
        _sharedActionsGroupRepository.Setup(s => s.AddOrUpdate(It.IsAny<IEnumerable<SharedActionsGroup>>()))
            .Callback<IEnumerable<SharedActionsGroup>>(groups => capturedGroups.Add(groups.ToList()));
        
        // sharedActionsGroupComputer = new SharedActionsGroupComputer();
        // sharedActionsGroups = sharedActionsGroupComputer.ComputeSharedActionsGroups(sharedAtomicActions);
        await _sharedActionsGroupComputer.ComputeSharedActionsGroups();
        
        Assert.That(capturedGroups.Count, Is.EqualTo(1));
        var capturedGroup = capturedGroups.Single().ToList();
        Assert.That(capturedGroup.Count, Is.EqualTo(1));
        var sharedActionGroup = capturedGroup.Single();
        Assert.That(sharedActionGroup.ActionsGroupId, Does.StartWith("AGID_"));
        Assert.That(sharedActionGroup.IsSynchronizeContent, Is.True);
        Assert.That(sharedActionGroup.IsSynchronizeContentAndDate, Is.True);
        Assert.That(sharedActionGroup.IsFinallySynchronizeContentAndDate, Is.True);
        Assert.That(sharedActionGroup.IsInitialOperatingOnSourceNeeded, Is.True);
        Assert.That(sharedActionGroup.NeedsOnlyOperatingOnTargets, Is.False);
        Assert.That(sharedActionGroup.IsFile, Is.True);
        Assert.That(sharedActionGroup.IsDirectory, Is.False);
        Assert.That(sharedActionGroup.IsCreate, Is.False);
        Assert.That(sharedActionGroup.IsDelete, Is.False);
        Assert.That(sharedActionGroup.IsDoNothing, Is.False);
        Assert.That(sharedActionGroup.IsSynchronizeContentOnly, Is.False);
        Assert.That(sharedActionGroup.IsSynchronizeDate, Is.False);
        Assert.That(sharedActionGroup.IsFinallySynchronizeDate, Is.False);
        Assert.That(sharedActionGroup.AppliesOnlySynchronizeDate, Is.False);
        Assert.That(sharedActionGroup.LinkingKeyValue, Is.EqualTo("file1.txt"));
        Assert.That(sharedActionGroup.Operator, Is.EqualTo(ActionOperatorTypes.SynchronizeContentAndDate));
        Assert.That(sharedActionGroup.PathIdentity, Is.EqualTo(sharedAtomicAction.PathIdentity));
        Assert.That(sharedActionGroup.Size, Is.EqualTo(10));
        Assert.That(sharedActionGroup.LastWriteTimeUtc, Is.EqualTo(dateTime));
        Assert.That(sharedActionGroup.Source, Is.EqualTo(sharedAtomicAction.Source));
        Assert.That(sharedActionGroup.Targets.Count, Is.EqualTo(1));
        Assert.That(sharedActionGroup.Targets.Contains(sharedAtomicAction.Target), Is.True);

        // _sharedActionsGroupRepository.Verify(s => s.AddSharedActionsGroups(It.IsAny<IEnumerable<SharedActionsGroup>>()), Times.Once);
        // _sharedActionsGroupRepository.Verify(s => s.AddSharedActionsGroups(
        //     It.Is<IEnumerable<SharedActionsGroup>>(groups => groups.All(g => g.IsSynchronizeContent))), 
        //     Times.Once);

        /*
        ClassicAssert.AreEqual(1, sharedActionsGroups.Count);
        sharedActionsGroup = sharedActionsGroups.Single();

        ClassicAssert.IsTrue(sharedActionsGroup.ActionsGroupId.StartsWith("AGID_"));

        ClassicAssert.IsFalse(sharedActionsGroup.AppliesOnlySynchronizeDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsCreate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDelete);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDirectory);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDoNothing);
        ClassicAssert.IsTrue (sharedActionsGroup.IsFile);
        ClassicAssert.IsTrue (sharedActionsGroup.IsFinallySynchronizeContentAndDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsFinallySynchronizeDate);
        ClassicAssert.IsTrue (sharedActionsGroup.IsInitialOperatingOnSourceNeeded);
        ClassicAssert.IsFalse(sharedActionsGroup.IsOnlyOperatingOnTargetsNeeded);
        ClassicAssert.IsTrue (sharedActionsGroup.IsSynchronizeContent);
        ClassicAssert.IsTrue (sharedActionsGroup.IsSynchronizeContentAndDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsSynchronizeContentOnly);
        ClassicAssert.IsFalse(sharedActionsGroup.IsSynchronizeDate);

        ClassicAssert.AreEqual(dateTime, sharedActionsGroup.LastWriteTimeUtc); 
        ClassicAssert.AreEqual("file1.txt", sharedActionsGroup.LinkingKeyValue);
        ClassicAssert.AreEqual(ActionOperatorTypes.SynchronizeContentAndDate, sharedActionsGroup.Operator);
        ClassicAssert.AreEqual(sharedAtomicAction.PathIdentity, sharedActionsGroup.PathIdentity);
        ClassicAssert.AreEqual(10, sharedActionsGroup.Size);
        ClassicAssert.AreEqual(sharedAtomicAction.Source, sharedActionsGroup.Source);
        ClassicAssert.AreEqual(1, sharedActionsGroup.Targets.Count);
        ClassicAssert.IsTrue(sharedActionsGroup.Targets.Contains(sharedAtomicAction.Target));

        */
    }
    
    [Test]
    public async Task Test_2Actions_1Source_2Targets_1()
    {
        SharedAtomicAction sharedAtomicAction1, sharedAtomicAction2;
        SharedActionsGroup sharedActionsGroup;

        var dateTime = DateTime.Now.AddMinutes(-10);

        sharedAtomicAction1 = new SharedAtomicAction("SAA1");
        sharedAtomicAction1.PathIdentity = new PathIdentity(FileSystemTypes.File, "file1.txt", "file1.txt", "file1.txt");
        sharedAtomicAction1.Source = new SharedDataPart("A1", FileSystemTypes.Directory, "CII_A", "A", "D:\\", "file1.txt", null, null, false);
        sharedAtomicAction1.Target = new SharedDataPart("B1", FileSystemTypes.Directory, "CII_B", "B", "D:\\", "file1.txt", null, null, false);
        sharedAtomicAction1.Operator = ActionOperatorTypes.SynchronizeContentAndDate;
        sharedAtomicAction1.SynchronizationType = SynchronizationTypes.Full;
        sharedAtomicAction1.Size = 10;
        sharedAtomicAction1.LastWriteTimeUtc = dateTime;


        sharedAtomicAction2 = new SharedAtomicAction("SAA2");
        sharedAtomicAction2.PathIdentity = new PathIdentity(FileSystemTypes.File, "file1.txt", "file1.txt", "file1.txt");
        sharedAtomicAction2.Source = new SharedDataPart("A1", FileSystemTypes.Directory, "CII_A", "A", "D:\\", "file1.txt", null, null, false);
        sharedAtomicAction2.Target = new SharedDataPart("C1", FileSystemTypes.Directory, "CII_C", "C", "D:\\", "file1.txt", null, null, false);
        sharedAtomicAction2.Operator = ActionOperatorTypes.SynchronizeContentAndDate;
        sharedAtomicAction2.SynchronizationType = SynchronizationTypes.Full;
        sharedAtomicAction2.Size = 10;
        sharedAtomicAction2.LastWriteTimeUtc = dateTime;


        var sharedAtomicActions = new List<SharedAtomicAction>();
        sharedAtomicActions.Add(sharedAtomicAction1);
        sharedAtomicActions.Add(sharedAtomicAction2);
        
        _sharedAtomicActionRepository.Setup(m => m.Elements).Returns(sharedAtomicActions);
        
        List<IEnumerable<SharedActionsGroup>> capturedGroups = new List<IEnumerable<SharedActionsGroup>>();
        _sharedActionsGroupRepository.Setup(s => s.AddOrUpdate(It.IsAny<IEnumerable<SharedActionsGroup>>()))
            .Callback<IEnumerable<SharedActionsGroup>>(groups => capturedGroups.Add(groups.ToList()));
        
        await _sharedActionsGroupComputer.ComputeSharedActionsGroups();
        
        Assert.That(capturedGroups.Count, Is.EqualTo(1));
        var sharedActionsGroups = capturedGroups.Single().ToList();
        
        ClassicAssert.AreEqual(1, sharedActionsGroups.Count);
        sharedActionsGroup = sharedActionsGroups.Single();

        ClassicAssert.IsTrue(sharedActionsGroup.ActionsGroupId.StartsWith("AGID_"));

        ClassicAssert.IsFalse(sharedActionsGroup.AppliesOnlySynchronizeDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsCreate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDelete);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDirectory);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDoNothing);
        ClassicAssert.IsTrue (sharedActionsGroup.IsFile);
        ClassicAssert.IsTrue (sharedActionsGroup.IsFinallySynchronizeContentAndDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsFinallySynchronizeDate);
        ClassicAssert.IsTrue (sharedActionsGroup.IsInitialOperatingOnSourceNeeded);
        ClassicAssert.IsFalse(sharedActionsGroup.NeedsOnlyOperatingOnTargets);
        ClassicAssert.IsTrue (sharedActionsGroup.IsSynchronizeContent);
        ClassicAssert.IsTrue (sharedActionsGroup.IsSynchronizeContentAndDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsSynchronizeContentOnly);
        ClassicAssert.IsFalse(sharedActionsGroup.IsSynchronizeDate);

        ClassicAssert.AreEqual(dateTime, sharedActionsGroup.LastWriteTimeUtc);
        ClassicAssert.AreEqual("file1.txt", sharedActionsGroup.LinkingKeyValue);
        ClassicAssert.AreEqual(ActionOperatorTypes.SynchronizeContentAndDate, sharedActionsGroup.Operator);
        ClassicAssert.AreEqual(sharedAtomicAction1.PathIdentity, sharedActionsGroup.PathIdentity);
        ClassicAssert.AreEqual(10, sharedActionsGroup.Size);
        ClassicAssert.AreEqual(sharedAtomicAction1.Source, sharedActionsGroup.Source);
        ClassicAssert.AreEqual(2, sharedActionsGroup.Targets.Count);
        ClassicAssert.IsTrue(sharedActionsGroup.Targets.Contains(sharedAtomicAction1.Target));
        ClassicAssert.IsTrue(sharedActionsGroup.Targets.Contains(sharedAtomicAction2.Target));
    }

    
    [Test]
    public async Task Test_2Actions_1Source_2Targets_2()
    {
        SharedAtomicAction sharedAtomicAction1, sharedAtomicAction2;
        SharedActionsGroup sharedActionsGroup;
        
        var dateTime = DateTime.Now.AddMinutes(-10);

        sharedAtomicAction1 = new SharedAtomicAction("SAA1");
        sharedAtomicAction1.PathIdentity = new PathIdentity(FileSystemTypes.File, "file1.txt", "file1.txt", "file1.txt");
        sharedAtomicAction1.Source = new SharedDataPart("A1", FileSystemTypes.Directory, "CII_A", "A", "D:\\", "file1.txt", "SigGuidA", "SigHashA", false);
        sharedAtomicAction1.Target = new SharedDataPart("B1", FileSystemTypes.Directory, "CII_B", "B", "D:\\", "file1.txt", "SigGuidB", "SigHashBC", false);
        sharedAtomicAction1.Operator = ActionOperatorTypes.SynchronizeContentAndDate;
        sharedAtomicAction1.SynchronizationType = SynchronizationTypes.Delta;
        sharedAtomicAction1.Size = 10;
        sharedAtomicAction1.LastWriteTimeUtc = dateTime;


        sharedAtomicAction2 = new SharedAtomicAction("SAA2");
        sharedAtomicAction2.PathIdentity = new PathIdentity(FileSystemTypes.File, "file1.txt", "file1.txt", "file1.txt");
        sharedAtomicAction2.Source = new SharedDataPart("A1", FileSystemTypes.Directory, "CII_A", "A", "D:\\", "file1.txt", "SigGuidA", "SigHashA", false);
        sharedAtomicAction2.Target = new SharedDataPart("C1", FileSystemTypes.Directory, "CII_C", "C", "D:\\", "file1.txt", "SigGuidC", "SigHashBC", false);
        sharedAtomicAction2.Operator = ActionOperatorTypes.SynchronizeContentAndDate;
        sharedAtomicAction2.SynchronizationType = SynchronizationTypes.Delta;
        sharedAtomicAction2.Size = 10;
        sharedAtomicAction2.LastWriteTimeUtc = dateTime;


        var sharedAtomicActions = new List<SharedAtomicAction>();
        sharedAtomicActions.Add(sharedAtomicAction1);
        sharedAtomicActions.Add(sharedAtomicAction2);
        
        _sharedAtomicActionRepository.Setup(m => m.Elements).Returns(sharedAtomicActions);
        
        List<IEnumerable<SharedActionsGroup>> capturedGroups = new List<IEnumerable<SharedActionsGroup>>();
        _sharedActionsGroupRepository.Setup(s => s.AddOrUpdate(It.IsAny<IEnumerable<SharedActionsGroup>>()))
            .Callback<IEnumerable<SharedActionsGroup>>(groups => capturedGroups.Add(groups.ToList()));
        
        await _sharedActionsGroupComputer.ComputeSharedActionsGroups();
        Assert.That(capturedGroups.Count, Is.EqualTo(1));
        var sharedActionsGroups = capturedGroups.Single().ToList();
        
        ClassicAssert.AreEqual(1, sharedActionsGroups.Count);
        sharedActionsGroup = sharedActionsGroups.Single();

        ClassicAssert.IsTrue(sharedActionsGroup.ActionsGroupId.StartsWith("AGID_"));

        ClassicAssert.IsFalse(sharedActionsGroup.AppliesOnlySynchronizeDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsCreate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDelete);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDirectory);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDoNothing);
        ClassicAssert.IsTrue (sharedActionsGroup.IsFile);
        ClassicAssert.IsTrue (sharedActionsGroup.IsFinallySynchronizeContentAndDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsFinallySynchronizeDate);
        ClassicAssert.IsTrue (sharedActionsGroup.IsInitialOperatingOnSourceNeeded);
        ClassicAssert.IsFalse(sharedActionsGroup.NeedsOnlyOperatingOnTargets);
        ClassicAssert.IsTrue (sharedActionsGroup.IsSynchronizeContent);
        ClassicAssert.IsTrue (sharedActionsGroup.IsSynchronizeContentAndDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsSynchronizeContentOnly);
        ClassicAssert.IsFalse(sharedActionsGroup.IsSynchronizeDate);

        ClassicAssert.AreEqual(dateTime, sharedActionsGroup.LastWriteTimeUtc);
        ClassicAssert.AreEqual("file1.txt", sharedActionsGroup.LinkingKeyValue);
        ClassicAssert.AreEqual(ActionOperatorTypes.SynchronizeContentAndDate, sharedActionsGroup.Operator);
        ClassicAssert.AreEqual(sharedAtomicAction1.PathIdentity, sharedActionsGroup.PathIdentity);
        ClassicAssert.AreEqual(10, sharedActionsGroup.Size);
        ClassicAssert.AreEqual(sharedAtomicAction1.Source, sharedActionsGroup.Source);
        ClassicAssert.AreEqual(2, sharedActionsGroup.Targets.Count);
        ClassicAssert.IsTrue(sharedActionsGroup.Targets.Contains(sharedAtomicAction1.Target));
        ClassicAssert.IsTrue(sharedActionsGroup.Targets.Contains(sharedAtomicAction2.Target));
    }

   
    [Test]
    public async Task Test_2Actions_1Source_2Targets_3()
    {
        SharedAtomicAction sharedAtomicAction1, sharedAtomicAction2;

        var dateTime = DateTime.Now.AddMinutes(-10);

        sharedAtomicAction1 = new SharedAtomicAction("SAA1");
        sharedAtomicAction1.PathIdentity = new PathIdentity(FileSystemTypes.File, "file1.txt", "file1.txt", "file1.txt");
        sharedAtomicAction1.Source = new SharedDataPart("A1", FileSystemTypes.Directory, "CII_A", "A", "D:\\", "file1.txt", "SigGuidA", "SigHashA", false);
        sharedAtomicAction1.Target = new SharedDataPart("B1", FileSystemTypes.Directory, "CII_B", "B", "D:\\", "file1.txt", "SigGuidB", "SigHashB", false);
        sharedAtomicAction1.Operator = ActionOperatorTypes.SynchronizeContentAndDate;
        sharedAtomicAction1.SynchronizationType = SynchronizationTypes.Delta;
        sharedAtomicAction1.Size = 10;
        sharedAtomicAction1.LastWriteTimeUtc = dateTime;


        sharedAtomicAction2 = new SharedAtomicAction("SAA2");
        sharedAtomicAction2.PathIdentity = new PathIdentity(FileSystemTypes.File, "file1.txt", "file1.txt", "file1.txt");
        sharedAtomicAction2.Source = new SharedDataPart("A1", FileSystemTypes.Directory, "CII_A", "A", "D:\\", "file1.txt", "SigGuidA", "SigHashA", false);
        sharedAtomicAction2.Target = new SharedDataPart("C1", FileSystemTypes.Directory, "CII_C", "C", "D:\\", "file1.txt", "SigGuidC", "SigHashC", false);
        sharedAtomicAction2.Operator = ActionOperatorTypes.SynchronizeContentAndDate;
        sharedAtomicAction2.SynchronizationType = SynchronizationTypes.Delta;
        sharedAtomicAction2.Size = 10;
        sharedAtomicAction2.LastWriteTimeUtc = dateTime;


        var sharedAtomicActions = new List<SharedAtomicAction>();
        sharedAtomicActions.Add(sharedAtomicAction1);
        sharedAtomicActions.Add(sharedAtomicAction2);

        _sharedAtomicActionRepository.Setup(m => m.Elements).Returns(sharedAtomicActions);
        
        List<IEnumerable<SharedActionsGroup>> capturedGroups = new List<IEnumerable<SharedActionsGroup>>();
        _sharedActionsGroupRepository.Setup(s => s.AddOrUpdate(It.IsAny<IEnumerable<SharedActionsGroup>>()))
            .Callback<IEnumerable<SharedActionsGroup>>(groups => capturedGroups.Add(groups.ToList()));
        
        await _sharedActionsGroupComputer.ComputeSharedActionsGroups();
        
        Assert.That(capturedGroups.Count, Is.EqualTo(1));
        var sharedActionsGroups = capturedGroups.Single().ToList();

        foreach (var sharedActionsGroup in sharedActionsGroups)
        {
            ClassicAssert.IsTrue(sharedActionsGroup.ActionsGroupId.StartsWith("AGID_"));

            ClassicAssert.IsFalse(sharedActionsGroup.AppliesOnlySynchronizeDate);
            ClassicAssert.IsFalse(sharedActionsGroup.IsCreate);
            ClassicAssert.IsFalse(sharedActionsGroup.IsDelete);
            ClassicAssert.IsFalse(sharedActionsGroup.IsDirectory);
            ClassicAssert.IsFalse(sharedActionsGroup.IsDoNothing);
            ClassicAssert.IsTrue (sharedActionsGroup.IsFile);
            ClassicAssert.IsTrue (sharedActionsGroup.IsFinallySynchronizeContentAndDate);
            ClassicAssert.IsFalse(sharedActionsGroup.IsFinallySynchronizeDate);
            ClassicAssert.IsTrue (sharedActionsGroup.IsInitialOperatingOnSourceNeeded);
            ClassicAssert.IsFalse(sharedActionsGroup.NeedsOnlyOperatingOnTargets);
            ClassicAssert.IsTrue (sharedActionsGroup.IsSynchronizeContent);
            ClassicAssert.IsTrue (sharedActionsGroup.IsSynchronizeContentAndDate);
            ClassicAssert.IsFalse(sharedActionsGroup.IsSynchronizeContentOnly);
            ClassicAssert.IsFalse(sharedActionsGroup.IsSynchronizeDate);

            ClassicAssert.AreEqual(dateTime, sharedActionsGroup.LastWriteTimeUtc);
            ClassicAssert.AreEqual("file1.txt", sharedActionsGroup.LinkingKeyValue);
            ClassicAssert.AreEqual(ActionOperatorTypes.SynchronizeContentAndDate, sharedActionsGroup.Operator);
            ClassicAssert.AreEqual(sharedAtomicAction1.PathIdentity, sharedActionsGroup.PathIdentity);
            ClassicAssert.AreEqual(10, sharedActionsGroup.Size);
            ClassicAssert.AreEqual(sharedAtomicAction1.Source, sharedActionsGroup.Source);
            ClassicAssert.AreEqual(1, sharedActionsGroup.Targets.Count);
        }
    }

    
    [Test]
    public async Task Test_2Actions_1Source_2Targets_4()
    {
        SharedAtomicAction sharedAtomicAction1, sharedAtomicAction2;
        SharedActionsGroup sharedActionsGroup;

        var dateTime = DateTime.Now.AddMinutes(-10);

        sharedAtomicAction1 = new SharedAtomicAction("SAA1");
        sharedAtomicAction1.PathIdentity = new PathIdentity(FileSystemTypes.File, "file1.txt", "file1.txt", "file1.txt");
        sharedAtomicAction1.Source = new SharedDataPart("A1", FileSystemTypes.Directory, "CII_A", "A", "D:\\", "file1.txt", "SigGuidA", "SigHashABC", false);
        sharedAtomicAction1.Target = new SharedDataPart("B1", FileSystemTypes.Directory, "CII_B", "B", "D:\\", "file1.txt", "SigGuidB", "SigHashABC", false);
        sharedAtomicAction1.Operator = ActionOperatorTypes.SynchronizeContentAndDate;
        sharedAtomicAction1.SynchronizationType = SynchronizationTypes.Delta;
        sharedAtomicAction1.Size = 10;
        sharedAtomicAction1.LastWriteTimeUtc = dateTime;


        sharedAtomicAction2 = new SharedAtomicAction("SAA2");
        sharedAtomicAction2.PathIdentity = new PathIdentity(FileSystemTypes.File, "file1.txt", "file1.txt", "file1.txt");
        sharedAtomicAction2.Source = new SharedDataPart("A1", FileSystemTypes.Directory, "CII_A", "A", "D:\\", "file1.txt", "SigGuidA", "SigHashABC", false);
        sharedAtomicAction2.Target = new SharedDataPart("C1", FileSystemTypes.Directory, "CII_C", "C", "D:\\", "file1.txt", "SigGuidC", "SigHashABC", false);
        sharedAtomicAction2.Operator = ActionOperatorTypes.SynchronizeContentAndDate;
        sharedAtomicAction2.SynchronizationType = SynchronizationTypes.Delta;
        sharedAtomicAction2.Size = 10;
        sharedAtomicAction2.LastWriteTimeUtc = dateTime;


        var sharedAtomicActions = new List<SharedAtomicAction>();
        sharedAtomicActions.Add(sharedAtomicAction1);
        sharedAtomicActions.Add(sharedAtomicAction2);

        _sharedAtomicActionRepository.Setup(m => m.Elements).Returns(sharedAtomicActions);
        
        List<IEnumerable<SharedActionsGroup>> capturedGroups = new List<IEnumerable<SharedActionsGroup>>();
        _sharedActionsGroupRepository.Setup(s => s.AddOrUpdate(It.IsAny<IEnumerable<SharedActionsGroup>>()))
            .Callback<IEnumerable<SharedActionsGroup>>(groups => capturedGroups.Add(groups.ToList()));
        
        await _sharedActionsGroupComputer.ComputeSharedActionsGroups();
        
        Assert.That(capturedGroups.Count, Is.EqualTo(1));
        var sharedActionsGroups = capturedGroups.Single().ToList();
        
        sharedActionsGroup = sharedActionsGroups.Single();

        ClassicAssert.IsTrue(sharedActionsGroup.ActionsGroupId.StartsWith("AGID_"));

        ClassicAssert.IsTrue (sharedActionsGroup.AppliesOnlySynchronizeDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsCreate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDelete);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDirectory);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDoNothing);
        ClassicAssert.IsTrue (sharedActionsGroup.IsFile);
        ClassicAssert.IsFalse(sharedActionsGroup.IsFinallySynchronizeContentAndDate);
        ClassicAssert.IsTrue (sharedActionsGroup.IsFinallySynchronizeDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsInitialOperatingOnSourceNeeded);
        ClassicAssert.IsTrue (sharedActionsGroup.NeedsOnlyOperatingOnTargets);
        ClassicAssert.IsTrue (sharedActionsGroup.IsSynchronizeContent);
        ClassicAssert.IsTrue (sharedActionsGroup.IsSynchronizeContentAndDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsSynchronizeContentOnly);
        ClassicAssert.IsFalse(sharedActionsGroup.IsSynchronizeDate);

        ClassicAssert.AreEqual(dateTime, sharedActionsGroup.LastWriteTimeUtc);
        ClassicAssert.AreEqual("file1.txt", sharedActionsGroup.LinkingKeyValue);
        ClassicAssert.AreEqual(ActionOperatorTypes.SynchronizeContentAndDate, sharedActionsGroup.Operator);
        ClassicAssert.AreEqual(sharedAtomicAction1.PathIdentity, sharedActionsGroup.PathIdentity);
        ClassicAssert.AreEqual(10, sharedActionsGroup.Size);
        ClassicAssert.AreEqual(sharedAtomicAction1.Source, sharedActionsGroup.Source);
        ClassicAssert.AreEqual(2, sharedActionsGroup.Targets.Count);
        ClassicAssert.IsTrue(sharedActionsGroup.Targets.Contains(sharedAtomicAction1.Target));
        ClassicAssert.IsTrue(sharedActionsGroup.Targets.Contains(sharedAtomicAction2.Target));
    }
    
    [Test]
    public async Task Test_2Actions_1Source_2Targets_5()
    {
        SharedAtomicAction sharedAtomicAction1, sharedAtomicAction2;
        SharedActionsGroup sharedActionsGroup;

        var dateTime = DateTime.Now.AddMinutes(-10);

        sharedAtomicAction1 = new SharedAtomicAction("SAA1");
        sharedAtomicAction1.PathIdentity = new PathIdentity(FileSystemTypes.File, "file1.txt", "file1.txt", "file1.txt");
        sharedAtomicAction1.Source = new SharedDataPart("A1", FileSystemTypes.Directory, "CII_A", "A", "D:\\", "file1.txt", "SigGuidA", "SigHashABC", false);
        sharedAtomicAction1.Target = null;
        sharedAtomicAction1.Operator = ActionOperatorTypes.SynchronizeContentAndDate;
        sharedAtomicAction1.SynchronizationType = SynchronizationTypes.Delta;
        sharedAtomicAction1.Size = 10;
        sharedAtomicAction1.LastWriteTimeUtc = dateTime;


        sharedAtomicAction2 = new SharedAtomicAction("SAA2");
        sharedAtomicAction2.PathIdentity = new PathIdentity(FileSystemTypes.File, "file1.txt", "file1.txt", "file1.txt");
        sharedAtomicAction2.Source = new SharedDataPart("A1", FileSystemTypes.Directory, "CII_A", "A", "D:\\", "file1.txt", "SigGuidA", "SigHashABC", false);
        sharedAtomicAction2.Target = new SharedDataPart("C1", FileSystemTypes.Directory, "CII_C", "C", "D:\\", "file1.txt", "SigGuidC", "SigHashABC", false);
        sharedAtomicAction2.Operator = ActionOperatorTypes.SynchronizeContentAndDate;
        sharedAtomicAction2.SynchronizationType = SynchronizationTypes.Delta;
        sharedAtomicAction2.Size = 10;
        sharedAtomicAction2.LastWriteTimeUtc = dateTime;


        var sharedAtomicActions = new List<SharedAtomicAction>();
        sharedAtomicActions.Add(sharedAtomicAction1);
        sharedAtomicActions.Add(sharedAtomicAction2);

        _sharedAtomicActionRepository.Setup(m => m.Elements).Returns(sharedAtomicActions);
        
        List<IEnumerable<SharedActionsGroup>> capturedGroups = new List<IEnumerable<SharedActionsGroup>>();
        _sharedActionsGroupRepository.Setup(s => s.AddOrUpdate(It.IsAny<IEnumerable<SharedActionsGroup>>()))
            .Callback<IEnumerable<SharedActionsGroup>>(groups => capturedGroups.Add(groups.ToList()));
        
        await _sharedActionsGroupComputer.ComputeSharedActionsGroups();
        
        Assert.That(capturedGroups.Count, Is.EqualTo(1));
        var sharedActionsGroups = capturedGroups.Single().ToList();
        
        sharedActionsGroup = sharedActionsGroups.Single();

        ClassicAssert.IsTrue(sharedActionsGroup.ActionsGroupId.StartsWith("AGID_"));

        ClassicAssert.IsTrue (sharedActionsGroup.AppliesOnlySynchronizeDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsCreate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDelete);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDirectory);
        ClassicAssert.IsFalse(sharedActionsGroup.IsDoNothing);
        ClassicAssert.IsTrue (sharedActionsGroup.IsFile);
        ClassicAssert.IsFalse(sharedActionsGroup.IsFinallySynchronizeContentAndDate);
        ClassicAssert.IsTrue (sharedActionsGroup.IsFinallySynchronizeDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsInitialOperatingOnSourceNeeded);
        ClassicAssert.IsTrue (sharedActionsGroup.NeedsOnlyOperatingOnTargets);
        ClassicAssert.IsTrue (sharedActionsGroup.IsSynchronizeContent);
        ClassicAssert.IsTrue (sharedActionsGroup.IsSynchronizeContentAndDate);
        ClassicAssert.IsFalse(sharedActionsGroup.IsSynchronizeContentOnly);
        ClassicAssert.IsFalse(sharedActionsGroup.IsSynchronizeDate);

        ClassicAssert.AreEqual(dateTime, sharedActionsGroup.LastWriteTimeUtc);
        ClassicAssert.AreEqual("file1.txt", sharedActionsGroup.LinkingKeyValue);
        ClassicAssert.AreEqual(ActionOperatorTypes.SynchronizeContentAndDate, sharedActionsGroup.Operator);
        ClassicAssert.AreEqual(sharedAtomicAction1.PathIdentity, sharedActionsGroup.PathIdentity);
        ClassicAssert.AreEqual(10, sharedActionsGroup.Size);
        ClassicAssert.AreEqual(sharedAtomicAction1.Source, sharedActionsGroup.Source);
        ClassicAssert.AreEqual(1, sharedActionsGroup.Targets.Count);
        ClassicAssert.IsTrue(sharedActionsGroup.Targets.Contains(sharedAtomicAction2.Target));
    }
}