using System.Reactive.Subjects;
using Autofac;
using System.Reactive.Linq;
using ByteSync.Business;
using ByteSync.Business.Sessions;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Inventories;
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
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ByteSync.Interfaces.Controls.Communications;

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
        _sessionService.SetupGet(s => s.SessionStatusObservable).Returns(System.Reactive.Linq.Observable.Never<SessionStatus>());

        _localizationService = new Mock<ILocalizationService>();
        _localizationService.Setup(ls => ls[It.IsAny<string>()]).Returns((string key) => key);

        _dialogService = new Mock<IDialogService>();
        _inventoryService = new Mock<IInventoryService>();
        _inventoryService.SetupGet(s => s.InventoryProcessData).Returns(new ByteSync.Business.Inventories.InventoryProcessData());

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
            Endpoint = new ByteSync.Common.Business.EndPoints.ByteSyncEndpoint
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
}
