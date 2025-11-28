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

namespace ByteSync.Client.UnitTests.ViewModels.Sessions.Comparisons.Results;

[TestFixture]
public class ContentIdentityViewModelTests
{
    private Mock<ISessionService> _sessionService = null!;
    private Mock<ILocalizationService> _localizationService = null!;
    private Mock<IDateAndInventoryPartsViewModelFactory> _factory = null!;
    
    private Inventory _inventory = null!;
    private InventoryPart _partA = null!;
    
    [SetUp]
    public void Setup()
    {
        _sessionService = new Mock<ISessionService>();
        _sessionService.SetupGet(s => s.CurrentSessionSettings).Returns(new SessionSettings
        {
            DataType = DataTypes.Files,
            MatchingMode = MatchingModes.Flat,
            LinkingCase = LinkingCases.Insensitive
        });
        
        var culture = new CultureDefinition(CultureInfo.InvariantCulture);
        _localizationService = new Mock<ILocalizationService>();
        _localizationService.SetupGet(l => l.CurrentCultureDefinition).Returns(culture);
        _localizationService.SetupGet(l => l.CurrentCultureObservable).Returns(Observable.Never<CultureDefinition>());
        _localizationService.Setup(ls => ls[It.IsAny<string>()]).Returns<string>(key => key);
        
        _factory = new Mock<IDateAndInventoryPartsViewModelFactory>();
        _factory.Setup(f =>
                f.CreateDateAndInventoryPartsViewModel(It.IsAny<ContentIdentityViewModel>(), It.IsAny<DateTime>(),
                    It.IsAny<HashSet<InventoryPart>>()))
            .Returns((ContentIdentityViewModel civm, DateTime dt, HashSet<InventoryPart> parts) =>
                new DateAndInventoryPartsViewModel(civm, dt, parts, _sessionService.Object, _localizationService.Object));
        
        _inventory = new Inventory
        {
            InventoryId = "INV_Aa",
            Code = "Aa",
            MachineName = "POW",
            Endpoint = new ByteSyncEndpoint
            {
                ClientInstanceId = "CII_A",
                OSPlatform = OSPlatforms.Windows
            }
        };
        _partA = new InventoryPart(_inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "Aa1" };
    }
    
    private static ComparisonItemViewModel BuildComparisonItemViewModel(FileSystemTypes type)
    {
        // We only need the FileSystemType and LinkingKeyNameTooltip read; the rest can be minimal.
        var comparisonItem = new ComparisonItem(new PathIdentity(type, "/p", "name", "/p"));
        var inventories = new List<Inventory>();
        
        var targetedActionsService = new Mock<ITargetedActionsService>().Object;
        
        // Real AtomicActionRepository to avoid null ObservableCache
        var sessionSvc = new Mock<ISessionService>();
        sessionSvc.SetupGet(s => s.SessionObservable).Returns(Observable.Never<AbstractSession?>());
        sessionSvc.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Never<SessionStatus>());
        var atomicRepo = new AtomicActionRepository(
            new SessionInvalidationCachePolicy<AtomicAction, string>(sessionSvc.Object),
            new PropertyIndexer<AtomicAction, ComparisonItem>());
        var ciFactory = new Mock<IContentIdentityViewModelFactory>().Object;
        var repartFactoryMock = new Mock<IContentRepartitionViewModelFactory>();
        repartFactoryMock.Setup(f => f.CreateContentRepartitionViewModel(It.IsAny<ComparisonItem>(), It.IsAny<List<Inventory>>()))
            .Returns(new ContentRepartitionViewModel());
        var repartFactory = repartFactoryMock.Object;
        
        var statusFactoryMock = new Mock<IItemSynchronizationStatusViewModelFactory>();
        statusFactoryMock.Setup(f => f.CreateItemSynchronizationStatusViewModel(It.IsAny<ComparisonItem>(), It.IsAny<List<Inventory>>()))
            .Returns(new ItemSynchronizationStatusViewModel());
        var statusFactory = statusFactoryMock.Object;
        var syncActionFactory = new Mock<ISynchronizationActionViewModelFactory>().Object;
        var format = new Mock<IFormatKbSizeConverter>().Object;
        
        return new ComparisonItemViewModel(targetedActionsService, atomicRepo, ciFactory, repartFactory, statusFactory, comparisonItem,
            inventories, syncActionFactory, format);
    }
    
    [Test]
    public void File_identity_without_hash_sets_empty_signature_and_regular_hash_icon()
    {
        var ciCore = new ContentIdentityCore { Size = 123 };
        var ci = new ContentIdentity(ciCore);
        
        var file = new FileDescription
        {
            InventoryPart = _partA, RelativePath = "/file.txt", Size = 10, CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow
        };
        ci.Add(file);
        
        var civm = new ContentIdentityViewModel(
            BuildComparisonItemViewModel(FileSystemTypes.File),
            ci,
            _inventory,
            _sessionService.Object,
            _localizationService.Object,
            _factory.Object);
        
        civm.SignatureHash.Should().Be("");
        civm.HashOrWarnIcon.Should().Be("RegularHash");
        civm.HasAnalysisError.Should().BeFalse();
        civm.ShowToolTipDelay.Should().Be(int.MaxValue);
    }
    
    [Test]
    public void File_identity_with_long_hash_is_truncated()
    {
        var core = new ContentIdentityCore { SignatureHash = new string('a', 57) + "/123/456", Size = 10 };
        var ci = new ContentIdentity(core);
        var file = new FileDescription
        {
            InventoryPart = _partA, RelativePath = "/file.txt", Size = 10, CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow
        };
        ci.Add(file);
        
        var vm = new ContentIdentityViewModel(
            BuildComparisonItemViewModel(FileSystemTypes.File),
            ci,
            _inventory,
            _sessionService.Object,
            _localizationService.Object,
            _factory.Object);
        
        vm.SignatureHash.Should().NotBeNull();
        vm.SignatureHash!.Length.Should().BeGreaterThan(10);
        vm.HashOrWarnIcon.Should().Be("RegularHash");
    }
    
    [Test]
    public void File_identity_with_analysis_error_sets_error_fields_and_tooltip_delay()
    {
        var core = new ContentIdentityCore { Size = 10 };
        var ci = new ContentIdentity(core);
        var file = new FileDescription
        {
            InventoryPart = _partA,
            RelativePath = "/file.txt",
            Size = 10,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            AnalysisErrorType = new string('E', 40),
            AnalysisErrorDescription = "error-desc"
        };
        ci.Add(file);
        
        var vm = new ContentIdentityViewModel(
            BuildComparisonItemViewModel(FileSystemTypes.File),
            ci,
            _inventory,
            _sessionService.Object,
            _localizationService.Object,
            _factory.Object);
        
        vm.HasAnalysisError.Should().BeTrue();
        vm.HashOrWarnIcon.Should().Be("RegularError");
        vm.ErrorType.Should().StartWith("E");
        vm.ErrorDescription.Should().Be("error-desc");
        vm.SignatureHash!.Should().EndWith("...");
        vm.SignatureHash!.Length.Should().Be(35);
        vm.ShowToolTipDelay.Should().Be(400);
    }
    
    [Test]
    public void File_identity_with_access_issue_sets_error_icon_and_tooltip_delay()
    {
        var ci = new ContentIdentity(null);
        var file = new FileDescription
        {
            InventoryPart = _partA,
            RelativePath = "/file.txt",
            Size = 10,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow
        };
        file.IsAccessible = false;
        ci.Add(file);
        
        var vm = new ContentIdentityViewModel(
            BuildComparisonItemViewModel(FileSystemTypes.File),
            ci,
            _inventory,
            _sessionService.Object,
            _localizationService.Object,
            _factory.Object);
        
        vm.HasAnalysisError.Should().BeFalse();
        vm.HashOrWarnIcon.Should().Be("RegularError");
        vm.ShowToolTipDelay.Should().Be(400);
    }
    
    [Test]
    public void File_identity_with_access_issue_and_incomplete_inventory_uses_inventory_incomplete_label()
    {
        var ci = new ContentIdentity(null);
        var file = new FileDescription
        {
            InventoryPart = _partA,
            RelativePath = "/file.txt",
            Size = 10,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = false
        };
        ci.Add(file);
        
        _partA.IsIncompleteDueToAccess = true;
        _inventory.InventoryParts.Add(_partA);
        
        var vm = new ContentIdentityViewModel(
            BuildComparisonItemViewModel(FileSystemTypes.File),
            ci,
            _inventory,
            _sessionService.Object,
            _localizationService.Object,
            _factory.Object);
        
        vm.HashOrWarnIcon.Should().Be("RegularError");
        vm.SignatureHash.Should().Be("ComparisonResult_InventoryIncomplete");
        vm.ShowToolTipDelay.Should().Be(400);
    }
    
    [Test]
    public void Directory_identity_sets_presence_parts_and_dates()
    {
        var ci = new ContentIdentity(null);
        var dir = new DirectoryDescription { InventoryPart = _partA, RelativePath = "/dir" };
        ci.Add(dir);
        
        // Also add a date registration for last write times to trigger factory call
        ci.InventoryPartsByLastWriteTimes.Add(DateTime.UtcNow, [_partA]);
        
        var vm = new ContentIdentityViewModel(
            BuildComparisonItemViewModel(FileSystemTypes.Directory),
            ci,
            _inventory,
            _sessionService.Object,
            _localizationService.Object,
            _factory.Object);
        
        vm.IsDirectory.Should().BeTrue();
        vm.PresenceParts.Should().Contain("Aa1");
        vm.DateAndInventoryParts.Should().NotBeEmpty();
    }
}
