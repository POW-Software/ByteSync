using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Avalonia.Media;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Business.Themes;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Filtering;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.Repositories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.Views.Misc;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Sessions.Comparisons.Results;

[TestFixture]
public class ComparisonResultViewModelTests
{
    private Mock<ISessionService> _sessionService = null!;
    private Mock<ILocalizationService> _localizationService = null!;
    private Mock<IDialogService> _dialogService = null!;
    private Mock<IInventoryService> _inventoryService = null!;
    private Mock<IComparisonItemsService> _comparisonItemsService = null!;
    private Mock<IComparisonItemViewModelFactory> _comparisonItemViewModelFactory = null!;
    private Mock<ISessionMemberRepository> _sessionMemberRepository = null!;
    private Mock<IFlyoutElementViewModelFactory> _flyoutElementViewModelFactory = null!;
    private IComparisonItemRepository _comparisonItemRepository = null!;
    private Mock<IFilterService> _filterService = null!;
    private Mock<IWebAccessor> _webAccessor = null!;
    private Mock<ILogger<ComparisonResultViewModel>> _logger = null!;
    
    private ReplaySubject<ComparisonResult?> _resultSubject = null!;
    
    [SetUp]
    public void Setup()
    {
        _sessionService = new Mock<ISessionService>();
        _sessionService.SetupGet(s => s.CurrentSessionSettings).Returns(SessionSettings.BuildDefault());
        _sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Never<SessionStatus>());
        
        _localizationService = new Mock<ILocalizationService>();
        _localizationService.Setup(ls => ls[It.IsAny<string>()]).Returns((string key) => key);
        
        _dialogService = new Mock<IDialogService>();
        _inventoryService = new Mock<IInventoryService>();
        _inventoryService.SetupGet(s => s.InventoryProcessData).Returns(new InventoryProcessData());
        
        _comparisonItemsService = new Mock<IComparisonItemsService>();
        _resultSubject = new ReplaySubject<ComparisonResult?>(1);
        _comparisonItemsService.SetupProperty(s => s.ComparisonResult, _resultSubject);
        _comparisonItemsService.SetupGet(s => s.ComparisonResult).Returns(_resultSubject);
        
        _comparisonItemViewModelFactory = new Mock<IComparisonItemViewModelFactory>();
        _sessionMemberRepository = new Mock<ISessionMemberRepository>();
        _sessionMemberRepository.SetupGet(r => r.IsCurrentUserFirstSessionMemberCurrentValue).Returns(true);
        
        _flyoutElementViewModelFactory = new Mock<IFlyoutElementViewModelFactory>();
        _comparisonItemRepository = new ComparisonItemRepository();
        _filterService = new Mock<IFilterService>();
        _filterService.Setup(f => f.BuildFilter(It.IsAny<List<string>>())).Returns(_ => true);
        _webAccessor = new Mock<IWebAccessor>();
        _logger = new Mock<ILogger<ComparisonResultViewModel>>();
    }
    
    private static Inventory BuildInventory(string code, string ci, string machine)
    {
        return new Inventory
        {
            InventoryId = $"INV_{code}",
            Code = code,
            MachineName = machine,
            Endpoint = new ByteSyncEndpoint
            {
                ClientInstanceId = ci,
                OSPlatform = OSPlatforms.Windows
            }
        };
    }
    
    [Test]
    public void WhenComparisonResultArrives_ColumnsVisibilityAndNamesAreSet()
    {
        var vm = new ComparisonResultViewModel(
            _sessionService.Object,
            _localizationService.Object,
            _dialogService.Object,
            _inventoryService.Object,
            _comparisonItemsService.Object,
            _comparisonItemViewModelFactory.Object,
            _sessionMemberRepository.Object,
            _flyoutElementViewModelFactory.Object,
            new ManageSynchronizationRulesViewModel(),
            _comparisonItemRepository,
            _filterService.Object,
            _webAccessor.Object,
            _logger.Object);
        
        vm.Activator.Activate();
        
        var result = new ComparisonResult();
        result.AddInventory(BuildInventory("Aa", "CII_A", "POW-25-EB01"));
        result.AddInventory(BuildInventory("Ba", "CII_B", "POW-25-EB01"));
        result.AddInventory(BuildInventory("Bb", "CII_B", "POW-25-EB01"));
        
        _resultSubject.OnNext(result);
        
        vm.AreResultsLoaded.Should().BeTrue();
        vm.IsColumn2Visible.Should().BeTrue();
        vm.IsColumn3Visible.Should().BeTrue();
        vm.IsColumn4Visible.Should().BeFalse();
        
        vm.Inventory1Name.Should().Contain("Aa");
        vm.Inventory2Name.Should().Contain("Ba");
        vm.Inventory3Name.Should().Contain("Bb");
    }
    
    [Test]
    public void WhenComparisonResultHasIncompleteParts_FlagsAreSetPerInventory()
    {
        var vm = new ComparisonResultViewModel(
            _sessionService.Object,
            _localizationService.Object,
            _dialogService.Object,
            _inventoryService.Object,
            _comparisonItemsService.Object,
            _comparisonItemViewModelFactory.Object,
            _sessionMemberRepository.Object,
            _flyoutElementViewModelFactory.Object,
            new ManageSynchronizationRulesViewModel(),
            _comparisonItemRepository,
            _filterService.Object,
            _webAccessor.Object,
            _logger.Object);
        
        vm.Activator.Activate();
        
        var result = new ComparisonResult();
        var inventoryA = BuildInventory("Aa", "CII_A", "POW-25-EB01");
        var inventoryB = BuildInventory("Ba", "CII_B", "POW-25-EB01");
        var incompletePart = new InventoryPart(inventoryB, "c:/rootB", FileSystemTypes.Directory) { Code = "B1", IsIncompleteDueToAccess = true };
        inventoryB.Add(incompletePart);
        var completePart = new InventoryPart(inventoryA, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        inventoryA.Add(completePart);
        
        result.AddInventory(inventoryA);
        result.AddInventory(inventoryB);
        
        _resultSubject.OnNext(result);
        
        vm.Inventory1HasIncompleteParts.Should().BeFalse();
        vm.Inventory2HasIncompleteParts.Should().BeTrue();
        vm.Inventory3HasIncompleteParts.Should().BeFalse();
    }
    
    [Test]
    public void BuildFilter_NoTags_ReturnsAlwaysTrue_AndDoesNotCallFilterService()
    {
        var vm = new ComparisonResultViewModel(
            _sessionService.Object,
            _localizationService.Object,
            _dialogService.Object,
            _inventoryService.Object,
            _comparisonItemsService.Object,
            _comparisonItemViewModelFactory.Object,
            _sessionMemberRepository.Object,
            _flyoutElementViewModelFactory.Object,
            new ManageSynchronizationRulesViewModel(),
            _comparisonItemRepository,
            _filterService.Object,
            _webAccessor.Object,
            _logger.Object);
        
        var buildFilter = typeof(ComparisonResultViewModel).GetMethod("BuildFilter", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var predicate = (Func<ComparisonItem, bool>)buildFilter.Invoke(vm, null)!;
        
        var item1 = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/a", "a", "/a"));
        var item2 = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/b", "b", "/b"));
        
        predicate(item1).Should().BeTrue();
        predicate(item2).Should().BeTrue();
        _filterService.Verify(f => f.BuildFilter(It.IsAny<List<string>>()), Times.Never);
    }
    
    [Test]
    public void BuildFilter_WithTags_DelegatesToFilterService_WithExtractedTexts()
    {
        // Arrange filter service to build a predicate based on provided tags
        _filterService
            .Setup(f => f.BuildFilter(It.IsAny<List<string>>()))
            .Returns<List<string>>(tags => (ComparisonItem ci) =>
                tags.All(t => ci.PathIdentity.LinkingKeyValue.Contains(t, StringComparison.OrdinalIgnoreCase)));
        
        var vm = new ComparisonResultViewModel(
            _sessionService.Object,
            _localizationService.Object,
            _dialogService.Object,
            _inventoryService.Object,
            _comparisonItemsService.Object,
            _comparisonItemViewModelFactory.Object,
            _sessionMemberRepository.Object,
            _flyoutElementViewModelFactory.Object,
            new ManageSynchronizationRulesViewModel(),
            _comparisonItemRepository,
            _filterService.Object,
            _webAccessor.Object,
            _logger.Object);
        
        // Inject one TagItem with text "foo"
        var filterParser = new Mock<IFilterParser>();
        filterParser.Setup(p => p.TryParse(It.IsAny<string>())).Returns(ParseResult.Incomplete(""));
        var themeSvc = new Mock<IThemeService>();
        themeSvc.SetupGet(t => t.SelectedTheme).Returns(Observable.Never<Theme>());
        themeSvc.Setup(t => t.GetBrush(It.IsAny<string>())).Returns((IBrush?)null);
        
        vm.FilterTags.Add(new TagItem(filterParser.Object, themeSvc.Object, "foo"));
        
        var buildFilter = typeof(ComparisonResultViewModel).GetMethod("BuildFilter", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var predicate = (Func<ComparisonItem, bool>)buildFilter.Invoke(vm, null)!;
        
        // Two items: only one contains tag "foo" in linking key value
        var match = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/dir/foo.txt", "foo.txt", "/dir/foo.txt"));
        var miss = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/dir/bar.txt", "bar.txt", "/dir/bar.txt"));
        
        predicate(match).Should().BeTrue();
        predicate(miss).Should().BeFalse();
        
        _filterService.Verify(f => f.BuildFilter(It.Is<List<string>>(l => l.Count == 1 && l[0] == "foo")), Times.Once);
    }
}
