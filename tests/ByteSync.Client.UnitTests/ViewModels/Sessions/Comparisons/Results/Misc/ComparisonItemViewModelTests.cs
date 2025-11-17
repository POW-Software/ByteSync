using System.Globalization;
using System.Reactive.Linq;
using ByteSync.Business;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Converters;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Repositories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Sessions.Comparisons.Results.Misc;

[TestFixture]
public class ComparisonItemViewModelTests
{
    private Mock<ITargetedActionsService> _targetedActionsService = null!;
    private IAtomicActionRepository _atomicActionRepository = null!;
    private Mock<IContentIdentityViewModelFactory> _contentIdentityViewModelFactory = null!;
    private Mock<IContentRepartitionViewModelFactory> _contentRepartitionViewModelFactory = null!;
    private Mock<IItemSynchronizationStatusViewModelFactory> _itemSynchronizationStatusViewModelFactory = null!;
    private Mock<ISynchronizationActionViewModelFactory> _synchronizationActionViewModelFactory = null!;
    private Mock<IFormatKbSizeConverter> _formatKbSizeConverter = null!;
    private Mock<ISessionService> _sessionService = null!;
    private Mock<ILocalizationService> _localizationService = null!;
    private Mock<IDateAndInventoryPartsViewModelFactory> _dateAndInventoryPartsViewModelFactory = null!;
    
    [SetUp]
    public void Setup()
    {
        _targetedActionsService = new Mock<ITargetedActionsService>();
        
        _sessionService = new Mock<ISessionService>();
        _sessionService.SetupGet(s => s.SessionObservable).Returns(Observable.Never<AbstractSession?>());
        _sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Never<SessionStatus>());
        _sessionService.SetupGet(s => s.CurrentSessionSettings).Returns(new SessionSettings
        {
            DataType = DataTypes.Files,
            MatchingMode = MatchingModes.Flat,
            LinkingCase = LinkingCases.Insensitive
        });
        _sessionService.SetupGet(s => s.IsCloudSession).Returns(false);
        
        _atomicActionRepository = new AtomicActionRepository(
            new SessionInvalidationCachePolicy<AtomicAction, string>(_sessionService.Object),
            new PropertyIndexer<AtomicAction, ComparisonItem>());
        
        var culture = new CultureDefinition(CultureInfo.InvariantCulture);
        _localizationService = new Mock<ILocalizationService>();
        _localizationService.SetupGet(l => l.CurrentCultureDefinition).Returns(culture);
        _localizationService.SetupGet(l => l.CurrentCultureObservable).Returns(Observable.Never<CultureDefinition>());
        
        _dateAndInventoryPartsViewModelFactory = new Mock<IDateAndInventoryPartsViewModelFactory>();
        _dateAndInventoryPartsViewModelFactory.Setup(f =>
                f.CreateDateAndInventoryPartsViewModel(It.IsAny<ContentIdentityViewModel>(), It.IsAny<DateTime>(),
                    It.IsAny<HashSet<InventoryPart>>()))
            .Returns((ContentIdentityViewModel civm, DateTime dt, HashSet<InventoryPart> parts) =>
                new DateAndInventoryPartsViewModel(civm, dt, parts, _sessionService.Object, _localizationService.Object));
        
        _contentIdentityViewModelFactory = new Mock<IContentIdentityViewModelFactory>();
        _contentRepartitionViewModelFactory = new Mock<IContentRepartitionViewModelFactory>();
        _contentRepartitionViewModelFactory
            .Setup(f => f.CreateContentRepartitionViewModel(It.IsAny<ComparisonItem>(), It.IsAny<List<Inventory>>()))
            .Returns(new ContentRepartitionViewModel());
        
        _itemSynchronizationStatusViewModelFactory = new Mock<IItemSynchronizationStatusViewModelFactory>();
        _itemSynchronizationStatusViewModelFactory
            .Setup(f => f.CreateItemSynchronizationStatusViewModel(It.IsAny<ComparisonItem>(), It.IsAny<List<Inventory>>()))
            .Returns(new ItemSynchronizationStatusViewModel());
        
        _synchronizationActionViewModelFactory = new Mock<ISynchronizationActionViewModelFactory>();
        _formatKbSizeConverter = new Mock<IFormatKbSizeConverter>();
    }
    
    private ComparisonItemViewModel CreateViewModel(ComparisonItem comparisonItem, List<Inventory> inventories)
    {
        return new ComparisonItemViewModel(
            _targetedActionsService.Object,
            _atomicActionRepository,
            _contentIdentityViewModelFactory.Object,
            _contentRepartitionViewModelFactory.Object,
            _itemSynchronizationStatusViewModelFactory.Object,
            comparisonItem,
            inventories,
            _synchronizationActionViewModelFactory.Object,
            _formatKbSizeConverter.Object);
    }
    
    private static Inventory CreateInventory(string code, string inventoryId, string machineName)
    {
        return new Inventory
        {
            InventoryId = inventoryId,
            Code = code,
            MachineName = machineName,
            Endpoint = new ByteSyncEndpoint
            {
                ClientInstanceId = $"CII_{code}",
                OSPlatform = OSPlatforms.Windows
            }
        };
    }
    
    private static InventoryPart CreateInventoryPart(Inventory inventory, string rootPath, string code)
    {
        return new InventoryPart(inventory, rootPath, FileSystemTypes.Directory) { Code = code };
    }
    
    [Test]
    public void Constructor_WithContentIdentities_CreatesContentIdentityViewModelsForEachInventory()
    {
        var inventoryA = CreateInventory("A", "INV_A", "MachineA");
        var inventoryB = CreateInventory("B", "INV_B", "MachineB");
        var inventories = new List<Inventory> { inventoryA, inventoryB };
        
        var partA = CreateInventoryPart(inventoryA, "c:/rootA", "A1");
        var partB = CreateInventoryPart(inventoryB, "c:/rootB", "B1");
        
        var contentIdentity1 = new ContentIdentity(new ContentIdentityCore { Size = 100 });
        var file1 = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file1.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        contentIdentity1.Add(file1);
        
        var contentIdentity2 = new ContentIdentity(new ContentIdentityCore { Size = 200 });
        var file2 = new FileDescription
        {
            InventoryPart = partB,
            RelativePath = "/file2.txt",
            Size = 200,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        contentIdentity2.Add(file2);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "name", "/p"));
        comparisonItem.AddContentIdentity(contentIdentity1);
        comparisonItem.AddContentIdentity(contentIdentity2);
        
        var createdViewModels = new List<ContentIdentityViewModel>();
        _contentIdentityViewModelFactory
            .Setup(f => f.CreateContentIdentityViewModel(It.IsAny<ComparisonItemViewModel>(), It.IsAny<ContentIdentity>(),
                It.IsAny<Inventory>()))
            .Returns((ComparisonItemViewModel parent, ContentIdentity ci, Inventory inv) =>
            {
                var vm = new ContentIdentityViewModel(parent, ci, inv, _sessionService.Object, _localizationService.Object,
                    _dateAndInventoryPartsViewModelFactory.Object);
                createdViewModels.Add(vm);
                
                return vm;
            });
        
        var viewModel = CreateViewModel(comparisonItem, inventories);
        
        createdViewModels.Should().HaveCount(2);
        viewModel.ContentIdentitiesA.Should().HaveCount(1);
        viewModel.ContentIdentitiesB.Should().HaveCount(1);
    }
    
    [Test]
    public void Constructor_WithContentIdentityInMultipleInventories_CreatesViewModelsForEachInventory()
    {
        var inventoryA = CreateInventory("A", "INV_A", "MachineA");
        var inventoryB = CreateInventory("B", "INV_B", "MachineB");
        var inventories = new List<Inventory> { inventoryA, inventoryB };
        
        var partA1 = CreateInventoryPart(inventoryA, "c:/rootA1", "A1");
        var partA2 = CreateInventoryPart(inventoryA, "c:/rootA2", "A2");
        var partB1 = CreateInventoryPart(inventoryB, "c:/rootB1", "B1");
        
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { Size = 100 });
        var file1 = new FileDescription
        {
            InventoryPart = partA1,
            RelativePath = "/file1.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        contentIdentity.Add(file1);
        
        var file2 = new FileDescription
        {
            InventoryPart = partA2,
            RelativePath = "/file2.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        contentIdentity.Add(file2);
        
        var file3 = new FileDescription
        {
            InventoryPart = partB1,
            RelativePath = "/file3.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        contentIdentity.Add(file3);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "name", "/p"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var createdViewModels = new List<ContentIdentityViewModel>();
        _contentIdentityViewModelFactory
            .Setup(f => f.CreateContentIdentityViewModel(It.IsAny<ComparisonItemViewModel>(), It.IsAny<ContentIdentity>(),
                It.IsAny<Inventory>()))
            .Returns((ComparisonItemViewModel parent, ContentIdentity ci, Inventory inv) =>
            {
                var vm = new ContentIdentityViewModel(parent, ci, inv, _sessionService.Object, _localizationService.Object,
                    _dateAndInventoryPartsViewModelFactory.Object);
                createdViewModels.Add(vm);
                
                return vm;
            });
        
        var viewModel = CreateViewModel(comparisonItem, inventories);
        
        createdViewModels.Should().HaveCount(2);
        viewModel.ContentIdentitiesA.Should().HaveCount(1);
        viewModel.ContentIdentitiesB.Should().HaveCount(1);
    }
    
    [Test]
    public void Constructor_SortsContentIdentitiesByInventoryPartCodes()
    {
        var inventoryA = CreateInventory("A", "INV_A", "MachineA");
        var inventories = new List<Inventory> { inventoryA };
        
        var partA2 = CreateInventoryPart(inventoryA, "c:/rootA2", "A2");
        var partA1 = CreateInventoryPart(inventoryA, "c:/rootA1", "A1");
        var partA3 = CreateInventoryPart(inventoryA, "c:/rootA3", "A3");
        
        var contentIdentity1 = new ContentIdentity(new ContentIdentityCore { Size = 100 });
        var file1 = new FileDescription
        {
            InventoryPart = partA2,
            RelativePath = "/file2.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        contentIdentity1.Add(file1);
        
        var contentIdentity2 = new ContentIdentity(new ContentIdentityCore { Size = 200 });
        var file2 = new FileDescription
        {
            InventoryPart = partA1,
            RelativePath = "/file1.txt",
            Size = 200,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        contentIdentity2.Add(file2);
        
        var contentIdentity3 = new ContentIdentity(new ContentIdentityCore { Size = 300 });
        var file3 = new FileDescription
        {
            InventoryPart = partA3,
            RelativePath = "/file3.txt",
            Size = 300,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        contentIdentity3.Add(file3);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "name", "/p"));
        comparisonItem.AddContentIdentity(contentIdentity1);
        comparisonItem.AddContentIdentity(contentIdentity2);
        comparisonItem.AddContentIdentity(contentIdentity3);
        
        var createdViewModels = new Dictionary<ContentIdentity, ContentIdentityViewModel>();
        _contentIdentityViewModelFactory
            .Setup(f => f.CreateContentIdentityViewModel(It.IsAny<ComparisonItemViewModel>(), It.IsAny<ContentIdentity>(),
                It.IsAny<Inventory>()))
            .Returns((ComparisonItemViewModel parent, ContentIdentity ci, Inventory inv) =>
            {
                var vm = new ContentIdentityViewModel(parent, ci, inv, _sessionService.Object, _localizationService.Object,
                    _dateAndInventoryPartsViewModelFactory.Object);
                createdViewModels[ci] = vm;
                
                return vm;
            });
        
        var viewModel = CreateViewModel(comparisonItem, inventories);
        
        viewModel.ContentIdentitiesA.Should().HaveCount(3);
        var sortedCodes = viewModel.ContentIdentitiesA
            .Select(vm => vm.ContentIdentity.GetInventoryParts()
                .Where(ip => ip.Inventory.Equals(inventoryA))
                .Min(ip => ip.Code))
            .ToList();
        
        sortedCodes.Should().BeInAscendingOrder();
        sortedCodes.Should().Equal("A1", "A2", "A3");
        
        createdViewModels.Should().HaveCount(3);
        createdViewModels.Keys.Should().BeEquivalentTo([contentIdentity1, contentIdentity2, contentIdentity3]);
        foreach (var vm in createdViewModels.Values)
        {
            viewModel.ContentIdentitiesA.Should().Contain(vm);
        }
    }
    
    [Test]
    public void GetContentIdentityViews_WithIndex0_ReturnsContentIdentitiesA()
    {
        var inventory = CreateInventory("A", "INV_A", "MachineA");
        var inventories = new List<Inventory> { inventory };
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "name", "/p"));
        
        var viewModel = CreateViewModel(comparisonItem, inventories);
        
        var result = viewModel.GetContentIdentityViews(inventory);
        
        result.Should().BeSameAs(viewModel.ContentIdentitiesA);
    }
    
    [Test]
    public void GetContentIdentityViews_WithIndex1_ReturnsContentIdentitiesB()
    {
        var inventoryA = CreateInventory("A", "INV_A", "MachineA");
        var inventoryB = CreateInventory("B", "INV_B", "MachineB");
        var inventories = new List<Inventory> { inventoryA, inventoryB };
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "name", "/p"));
        
        var viewModel = CreateViewModel(comparisonItem, inventories);
        
        var result = viewModel.GetContentIdentityViews(inventoryB);
        
        result.Should().BeSameAs(viewModel.ContentIdentitiesB);
    }
    
    [Test]
    public void GetContentIdentityViews_WithIndex2_ReturnsContentIdentitiesC()
    {
        var inventoryA = CreateInventory("A", "INV_A", "MachineA");
        var inventoryB = CreateInventory("B", "INV_B", "MachineB");
        var inventoryC = CreateInventory("C", "INV_C", "MachineC");
        var inventories = new List<Inventory> { inventoryA, inventoryB, inventoryC };
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "name", "/p"));
        
        var viewModel = CreateViewModel(comparisonItem, inventories);
        
        var result = viewModel.GetContentIdentityViews(inventoryC);
        
        result.Should().BeSameAs(viewModel.ContentIdentitiesC);
    }
    
    [Test]
    public void GetContentIdentityViews_WithIndex3_ReturnsContentIdentitiesD()
    {
        var inventoryA = CreateInventory("A", "INV_A", "MachineA");
        var inventoryB = CreateInventory("B", "INV_B", "MachineB");
        var inventoryC = CreateInventory("C", "INV_C", "MachineC");
        var inventoryD = CreateInventory("D", "INV_D", "MachineD");
        var inventories = new List<Inventory> { inventoryA, inventoryB, inventoryC, inventoryD };
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "name", "/p"));
        
        var viewModel = CreateViewModel(comparisonItem, inventories);
        
        var result = viewModel.GetContentIdentityViews(inventoryD);
        
        result.Should().BeSameAs(viewModel.ContentIdentitiesD);
    }
    
    [Test]
    public void GetContentIdentityViews_WithIndex4_ReturnsContentIdentitiesE()
    {
        var inventoryA = CreateInventory("A", "INV_A", "MachineA");
        var inventoryB = CreateInventory("B", "INV_B", "MachineB");
        var inventoryC = CreateInventory("C", "INV_C", "MachineC");
        var inventoryD = CreateInventory("D", "INV_D", "MachineD");
        var inventoryE = CreateInventory("E", "INV_E", "MachineE");
        var inventories = new List<Inventory> { inventoryA, inventoryB, inventoryC, inventoryD, inventoryE };
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "name", "/p"));
        
        var viewModel = CreateViewModel(comparisonItem, inventories);
        
        var result = viewModel.GetContentIdentityViews(inventoryE);
        
        result.Should().BeSameAs(viewModel.ContentIdentitiesE);
    }
    
    [Test]
    public void GetContentIdentityViews_WithInvalidIndex_ThrowsApplicationException()
    {
        var inventoryA = CreateInventory("A", "INV_A", "MachineA");
        var inventoryB = CreateInventory("B", "INV_B", "MachineB");
        var inventoryC = CreateInventory("C", "INV_C", "MachineC");
        var inventoryD = CreateInventory("D", "INV_D", "MachineD");
        var inventoryE = CreateInventory("E", "INV_E", "MachineE");
        var inventoryF = CreateInventory("F", "INV_F", "MachineF");
        var inventories = new List<Inventory> { inventoryA, inventoryB, inventoryC, inventoryD, inventoryE };
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "name", "/p"));
        
        var viewModel = CreateViewModel(comparisonItem, inventories);
        
        Action act = () => viewModel.GetContentIdentityViews(inventoryF);
        
        act.Should().Throw<ApplicationException>()
            .WithMessage("GetContentIdentityViews: can not identify ContentIdentities, -1:*");
    }
    
    [Test]
    public void GetContentIdentityViews_WithInventoryNotInList_ThrowsApplicationException()
    {
        var inventoryA = CreateInventory("A", "INV_A", "MachineA");
        var inventoryB = CreateInventory("B", "INV_B", "MachineB");
        var inventories = new List<Inventory> { inventoryA };
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/p", "name", "/p"));
        
        var viewModel = CreateViewModel(comparisonItem, inventories);
        
        Action act = () => viewModel.GetContentIdentityViews(inventoryB);
        
        act.Should().Throw<ApplicationException>()
            .WithMessage("GetContentIdentityViews: can not identify ContentIdentities, -1:*");
    }
}