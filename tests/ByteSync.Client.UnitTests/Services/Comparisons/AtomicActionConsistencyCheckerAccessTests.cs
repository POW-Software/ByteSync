using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Comparisons;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Comparisons;

[TestFixture]
public class AtomicActionConsistencyCheckerAccessTests
{
    private static ComparisonItem BuildComparisonItem(InventoryPart src, InventoryPart dst, bool sourceAccessible, bool targetAccessible)
    {
        var item = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "p", "/p"));
        
        // Source identity
        var srcCi = new ContentIdentity(null);
        var srcFd = new FileDescription
            { InventoryPart = src, RelativePath = "/p", Size = 1, CreationTimeUtc = DateTime.UtcNow, LastWriteTimeUtc = DateTime.UtcNow };
        srcFd.IsAccessible = sourceAccessible;
        srcCi.Add(srcFd);
        item.AddContentIdentity(srcCi);
        
        // Target identity
        var dstCi = new ContentIdentity(null);
        var dstFd = new FileDescription
            { InventoryPart = dst, RelativePath = "/p", Size = 1, CreationTimeUtc = DateTime.UtcNow, LastWriteTimeUtc = DateTime.UtcNow };
        dstFd.IsAccessible = targetAccessible;
        dstCi.Add(dstFd);
        item.AddContentIdentity(dstCi);
        
        return item;
    }
    
    private static (InventoryPart src, InventoryPart dst) BuildParts()
    {
        var invA = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var invB = new Inventory { InventoryId = "INV_B", Code = "B", Endpoint = new(), MachineName = "M" };
        var src = new InventoryPart(invA, "c:/a", FileSystemTypes.Directory) { Code = "A1" };
        var dst = new InventoryPart(invB, "c:/b", FileSystemTypes.Directory) { Code = "B1" };
        
        return (src, dst);
    }
    
    private static AtomicActionConsistencyChecker BuildChecker(Mock<IAtomicActionRepository> repoMock, MatchingModes matchingMode = MatchingModes.Tree)
    {
        var sessionServiceMock = new Mock<ISessionService>();
        sessionServiceMock.SetupGet(s => s.CurrentSessionSettings).Returns(new SessionSettings { MatchingMode = matchingMode });
        
        return new AtomicActionConsistencyChecker(repoMock.Object, sessionServiceMock.Object);
    }
    
    [Test]
    public void Synchronize_Fails_When_Source_Not_Accessible()
    {
        var (src, dst) = BuildParts();
        var item = BuildComparisonItem(src, dst, sourceAccessible: false, targetAccessible: true);
        
        var action = new AtomicAction
        {
            Operator = ActionOperatorTypes.SynchronizeContentOnly,
            Source = new DataPart("A", src),
            Destination = new DataPart("B", dst),
            ComparisonItem = item
        };
        
        var repoMock = new Mock<IAtomicActionRepository>();
        repoMock.Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>())).Returns(new List<AtomicAction>());
        var checker = BuildChecker(repoMock);
        var result = checker.CheckCanAdd(action, item);
        
        result.ValidationResults.Should().HaveCount(1);
        result.ValidationResults[0].IsValid.Should().BeFalse();
        result.ValidationResults[0].FailureReason.Should().Be(AtomicActionValidationFailureReason.SourceNotAccessible);
    }
    
    [Test]
    public void Synchronize_Fails_When_Source_Part_Incomplete_In_Flat_Mode()
    {
        var (src, dst) = BuildParts();
        src.IsIncompleteDueToAccess = true;
        var item = BuildComparisonItem(src, dst, sourceAccessible: true, targetAccessible: true);
        
        var action = new AtomicAction
        {
            Operator = ActionOperatorTypes.SynchronizeContentOnly,
            Source = new DataPart("A", src),
            Destination = new DataPart("B", dst),
            ComparisonItem = item
        };
        
        var repoMock = new Mock<IAtomicActionRepository>();
        repoMock.Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>())).Returns(new List<AtomicAction>());
        var checker = BuildChecker(repoMock, MatchingModes.Flat);
        var result = checker.CheckCanAdd(action, item);
        
        result.ValidationResults.Should().HaveCount(1);
        result.ValidationResults[0].IsValid.Should().BeFalse();
        result.ValidationResults[0].FailureReason.Should().Be(AtomicActionValidationFailureReason.SourceNotAccessible);
    }
    
    [Test]
    public void Synchronize_WithSourceCoreNull_DoesNotThrowException()
    {
        var (src, dst) = BuildParts();
        var item = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "p", "/p"));
        
        var srcCi = new ContentIdentity(null);
        var srcFd = new FileDescription
        {
            InventoryPart = src,
            RelativePath = "/p",
            Size = 1,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        srcCi.Add(srcFd);
        item.AddContentIdentity(srcCi);
        
        var dstCi = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash", Size = 1 });
        var dstFd = new FileDescription
        {
            InventoryPart = dst,
            RelativePath = "/p",
            Size = 1,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        dstCi.Add(dstFd);
        item.AddContentIdentity(dstCi);
        
        var action = new AtomicAction
        {
            Operator = ActionOperatorTypes.SynchronizeContentAndDate,
            Source = new DataPart("A", src),
            Destination = new DataPart("B", dst),
            ComparisonItem = item
        };
        
        var repoMock = new Mock<IAtomicActionRepository>();
        repoMock.Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>())).Returns(new List<AtomicAction>());
        var checker = BuildChecker(repoMock);
        
        Action act = () => checker.CheckCanAdd(action, item);
        
        act.Should().NotThrow();
    }
    
    [Test]
    public void Synchronize_Fails_When_Target_Part_Incomplete_In_Flat_Mode()
    {
        var (src, dst) = BuildParts();
        dst.IsIncompleteDueToAccess = true;
        var item = BuildComparisonItem(src, dst, sourceAccessible: true, targetAccessible: true);
        
        var action = new AtomicAction
        {
            Operator = ActionOperatorTypes.SynchronizeContentOnly,
            Source = new DataPart("A", src),
            Destination = new DataPart("B", dst),
            ComparisonItem = item
        };
        
        var repoMock = new Mock<IAtomicActionRepository>();
        repoMock.Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>())).Returns(new List<AtomicAction>());
        var checker = BuildChecker(repoMock, MatchingModes.Flat);
        var result = checker.CheckCanAdd(action, item);
        
        result.ValidationResults.Should().HaveCount(1);
        result.ValidationResults[0].IsValid.Should().BeFalse();
        result.ValidationResults[0].FailureReason.Should().Be(AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible);
    }
    
    [Test]
    public void Synchronize_WithTargetCoreNull_DoesNotThrowException()
    {
        var (src, dst) = BuildParts();
        var item = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "p", "/p"));
        
        var srcCi = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash", Size = 1 });
        var srcFd = new FileDescription
        {
            InventoryPart = src,
            RelativePath = "/p",
            Size = 1,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        srcCi.Add(srcFd);
        item.AddContentIdentity(srcCi);
        
        var dstCi = new ContentIdentity(null);
        var dstFd = new FileDescription
        {
            InventoryPart = dst,
            RelativePath = "/p",
            Size = 1,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        dstCi.Add(dstFd);
        item.AddContentIdentity(dstCi);
        
        var action = new AtomicAction
        {
            Operator = ActionOperatorTypes.SynchronizeContentAndDate,
            Source = new DataPart("A", src),
            Destination = new DataPart("B", dst),
            ComparisonItem = item
        };
        
        var repoMock = new Mock<IAtomicActionRepository>();
        repoMock.Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>())).Returns(new List<AtomicAction>());
        var checker = BuildChecker(repoMock);
        
        Action act = () => checker.CheckCanAdd(action, item);
        
        act.Should().NotThrow();
    }
    
    [Test]
    public void SynchronizeContentOnly_WithBothCoresNull_DoesNotThrowException()
    {
        var (src, dst) = BuildParts();
        var item = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "p", "/p"));
        
        var srcCi = new ContentIdentity(null);
        var srcFd = new FileDescription
        {
            InventoryPart = src,
            RelativePath = "/p",
            Size = 1,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        srcCi.Add(srcFd);
        item.AddContentIdentity(srcCi);
        
        var dstCi = new ContentIdentity(null);
        var dstFd = new FileDescription
        {
            InventoryPart = dst,
            RelativePath = "/p",
            Size = 1,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        dstCi.Add(dstFd);
        item.AddContentIdentity(dstCi);
        
        var action = new AtomicAction
        {
            Operator = ActionOperatorTypes.SynchronizeContentOnly,
            Source = new DataPart("A", src),
            Destination = new DataPart("B", dst),
            ComparisonItem = item
        };
        
        var repoMock = new Mock<IAtomicActionRepository>();
        repoMock.Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>())).Returns(new List<AtomicAction>());
        var checker = BuildChecker(repoMock);
        
        Action act = () => checker.CheckCanAdd(action, item);
        
        act.Should().NotThrow();
    }
    
    [Test]
    public void Synchronize_Allows_Incomplete_Parts_In_Tree_Mode_When_Items_Accessible()
    {
        var (src, dst) = BuildParts();
        src.IsIncompleteDueToAccess = true;
        dst.IsIncompleteDueToAccess = true;
        var item = BuildComparisonItem(src, dst, sourceAccessible: true, targetAccessible: true);
        
        var action = new AtomicAction
        {
            Operator = ActionOperatorTypes.SynchronizeContentOnly,
            Source = new DataPart("A", src),
            Destination = new DataPart("B", dst),
            ComparisonItem = item
        };
        
        var repoMock = new Mock<IAtomicActionRepository>();
        repoMock.Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>())).Returns(new List<AtomicAction>());
        var checker = BuildChecker(repoMock, MatchingModes.Tree);
        var result = checker.CheckCanAdd(action, item);
        
        result.ValidationResults.Should().HaveCount(1);
        result.ValidationResults[0].IsValid.Should().BeTrue();
    }
    
    [Test]
    public void Synchronize_WithInaccessibleTargetAndNullCore_DoesNotThrowException()
    {
        var (src, dst) = BuildParts();
        var item = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "p", "/p"));
        
        var srcCi = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash", Size = 1 });
        var srcFd = new FileDescription
        {
            InventoryPart = src,
            RelativePath = "/p",
            Size = 1,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        srcCi.Add(srcFd);
        item.AddContentIdentity(srcCi);
        
        var dstCi = new ContentIdentity(null);
        var dstFd = new FileDescription
        {
            InventoryPart = dst,
            RelativePath = "/p",
            Size = 1,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = false
        };
        dstCi.Add(dstFd);
        item.AddContentIdentity(dstCi);
        
        var action = new AtomicAction
        {
            Operator = ActionOperatorTypes.SynchronizeContentAndDate,
            Source = new DataPart("A", src),
            Destination = new DataPart("B", dst),
            ComparisonItem = item
        };
        
        var repoMock = new Mock<IAtomicActionRepository>();
        repoMock.Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>())).Returns(new List<AtomicAction>());
        var checker = BuildChecker(repoMock);
        var result = checker.CheckCanAdd(action, item);
        
        result.ValidationResults.Should().HaveCount(1);
        result.ValidationResults[0].IsValid.Should().BeFalse();
        result.ValidationResults[0].FailureReason.Should().Be(AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible);
    }
    
    [Test]
    public void Delete_Fails_When_Target_Part_Incomplete_In_Flat_Mode()
    {
        var (src, dst) = BuildParts();
        dst.IsIncompleteDueToAccess = true;
        var item = BuildComparisonItem(src, dst, sourceAccessible: true, targetAccessible: true);
        
        var action = new AtomicAction
        {
            Operator = ActionOperatorTypes.Delete,
            Destination = new DataPart("B", dst),
            ComparisonItem = item
        };
        
        var repoMock = new Mock<IAtomicActionRepository>();
        repoMock.Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>())).Returns(new List<AtomicAction>());
        var checker = BuildChecker(repoMock, MatchingModes.Flat);
        var result = checker.CheckCanAdd(action, item);
        
        result.ValidationResults.Should().HaveCount(1);
        result.ValidationResults[0].IsValid.Should().BeFalse();
        result.ValidationResults[0].FailureReason.Should().Be(AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible);
    }
    
    [Test]
    public void Create_Fails_When_Target_Part_Incomplete_In_Flat_Mode()
    {
        var (src, dst) = BuildParts();
        dst.InventoryPartType = FileSystemTypes.Directory;
        dst.IsIncompleteDueToAccess = true;
        var item = new ComparisonItem(new PathIdentity(FileSystemTypes.Directory, "/p", "p", "/p"));
        
        var action = new AtomicAction
        {
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("B", dst),
            ComparisonItem = item
        };
        
        var repoMock = new Mock<IAtomicActionRepository>();
        repoMock.Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>())).Returns(new List<AtomicAction>());
        var checker = BuildChecker(repoMock, MatchingModes.Flat);
        var result = checker.CheckCanAdd(action, item);
        
        result.ValidationResults.Should().HaveCount(1);
        result.ValidationResults[0].IsValid.Should().BeFalse();
        result.ValidationResults[0].FailureReason.Should().Be(AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible);
    }
    
    // Note: Additional tests for multi-target scenarios and various action operators
    // are covered by integration tests. The unit test above covers the core access control
    // for inaccessible sources.
}
