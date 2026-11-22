using System.Globalization;
using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using Autofac;
using Avalonia.Media;
using AwesomeAssertions;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Serials;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Services;
using ByteSync.ViewModels.AccountDetails;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using Moq;
using NUnit.Framework;
using ReactiveUI;

namespace ByteSync.Client.UnitTests.ViewModels.AccountDetails;

[TestFixture]
[NonParallelizable]
[SetCulture("en-US")]
[SetUICulture("en-US")]
public class UsageStatisticsViewModelTests
{
    private Mock<IStatisticsService> _statisticsService = null!;
    private Mock<ILocalizationService> _localizationService = null!;
    private UsageStatisticsData _data = null!;
    private UsageStatisticsViewModel _viewModel = null!;
    private IContainer _previousContainer = null!;
    private IContainer _container = null!;
    private IDisposable _activation = null!;
    private IScheduler _previousScheduler = null!;

    [SetUp]
    public void SetUp()
    {
        _previousScheduler = RxApp.MainThreadScheduler;
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;
        _statisticsService = new Mock<IStatisticsService>();
        _localizationService = new Mock<ILocalizationService>();
        _localizationService.Setup(service => service.GetMonthName(It.IsAny<int>()))
            .Returns((int index) => CultureInfo.GetCultureInfo("en-US").DateTimeFormat.GetMonthName(index + 1));
        _localizationService.Setup(service => service[It.IsAny<string>()])
            .Returns((string key) => key switch
            {
                "Misc_SizeUnit_Byte" => "B",
                "Misc_SizeUnitTemplate_Byte" => "{0} B",
                "Misc_SizeUnitTemplate_KiloByte" => "{0} KB",
                "Misc_SizeUnitTemplate_MegaByte" => "{0} MB",
                _ => key
            });

        _previousContainer = ContainerProvider.Container;
        var builder = new ContainerBuilder();
        builder.RegisterInstance(_localizationService.Object).As<ILocalizationService>();
        _container = builder.Build();
        ContainerProvider.Container = _container;

        var themeService = new Mock<IThemeService>();
        themeService.Setup(service => service.GetBrush(It.IsAny<string>()))
            .Returns(new SolidColorBrush(Colors.CornflowerBlue));
        var settings = new Mock<IApplicationSettingsRepository>();
        settings.SetupGet(repository => repository.ProductSerialDescription)
            .Returns(new ProductSerialDescription { AllowedCloudSynchronizationVolumeInBytes = 1024 });

        _data = new UsageStatisticsData(CreatePeriod(), CreatePeriod());
        _statisticsService.Setup(service => service.GetUsageStatistics(It.IsAny<UsageStatisticsRequest>()))
            .ReturnsAsync(() => _data);
        _viewModel = new UsageStatisticsViewModel(_statisticsService.Object, _localizationService.Object,
            themeService.Object, settings.Object);
        _activation = _viewModel.Activator.Activate();
    }

    [TearDown]
    public void TearDown()
    {
        _activation.Dispose();
        _viewModel.PreviousPeriodCommand.Dispose();
        _viewModel.NextPeriodCommand.Dispose();
        ContainerProvider.Container = _previousContainer;
        _container.Dispose();
        RxApp.MainThreadScheduler = _previousScheduler;
    }

    [Test]
    public async Task PreviousPeriod_ShouldRequestSelectedYearAndBuildMonthlySeries()
    {
        var initialYear = _viewModel.Year;

        await _viewModel.PreviousPeriodCommand.Execute().ToTask();

        _viewModel.Year.Should().Be(initialYear - 1);
        _viewModel.PreviousYear.Should().Be(initialYear - 2);
        _statisticsService.Verify(service => service.GetUsageStatistics(
            It.Is<UsageStatisticsRequest>(request => request.Year == initialYear - 1)), Times.Once);
        _viewModel.Series.Should().ContainSingle();
        _viewModel.Series![0].Values!.Cast<object>().Should().HaveCount(12);
        _viewModel.XAxes.Should().ContainSingle();
        _viewModel.XAxes![0].Labels.Should().Equal("J", "F", "M", "A", "M", "J", "J", "A", "S", "O", "N", "D");
        _viewModel.XAxes[0].MinLimit.Should().Be(-1);
        _viewModel.XAxes[0].MaxLimit.Should().Be(12);
        _viewModel.Sections.Should().BeEmpty();
    }

    [TestCase(0, false, 32768L, 32768d)]
    [TestCase(1, false, 1048576L, 1048576d)]
    [TestCase(0, true, 32768L, 8d)]
    [TestCase(1, true, 1048576L, 16d)]
    [TestCase(0, false, 0L, 0d)]
    [TestCase(1, true, 0L, 0d)]
    public async Task SeriesMapping_ShouldPreserveMonthAndApplySelectedScale(
        int seriesIndex, bool progressive, long volume, double expectedValue)
    {
        const int monthIndex = 3;
        _data.CurrentPeriodData.UploadedVolumePerSubPeriod[monthIndex].UploadedVolume = volume;
        _data.PreviousPeriodData.UploadedVolumePerSubPeriod[monthIndex].UploadedVolume = volume;
        await _viewModel.PreviousPeriodCommand.Execute().ToTask();
        _viewModel.ShowPreviousPeriod = true;
        _viewModel.UseProgressiveScale = progressive;
        var series = _viewModel.Series![seriesIndex];
        var model = series.Values!.Cast<object>().ElementAt(monthIndex);
        var mapping = (Delegate)series.GetType().GetProperty("Mapping")!.GetValue(series)!;

        var coordinate = (Coordinate)mapping.DynamicInvoke(model, 99)!;

        coordinate.SecondaryValue.Should().Be(monthIndex);
        coordinate.PrimaryValue.Should().BeApproximately(expectedValue, 0.000001);
    }

    [TestCase(0, 0, 0L, "0 B")]
    [TestCase(0, 1, 512L, "512 B")]
    [TestCase(0, 11, 1024L, "1,024 B")]
    [TestCase(0, 1, 1025L, "1.00 KB (1,025 B)")]
    [TestCase(1, 0, 512L, "512 B")]
    [TestCase(1, 11, 1048576L, "1.00 MB (1,048,576 B)")]
    public async Task SeriesTooltip_ShouldShowLocalizedMonthCorrectYearAndOriginalVolume(
        int seriesIndex, int monthIndex, long volume, string expectedVolume)
    {
        _data.CurrentPeriodData.UploadedVolumePerSubPeriod[monthIndex].UploadedVolume = volume;
        _data.PreviousPeriodData.UploadedVolumePerSubPeriod[monthIndex].UploadedVolume = volume;
        await _viewModel.PreviousPeriodCommand.Execute().ToTask();
        _viewModel.ShowPreviousPeriod = true;
        var series = _viewModel.Series![seriesIndex];
        var entity = new MappedChartEntity
        {
            MetaData = new ChartEntityMetaData(_ => { }) { EntityIndex = monthIndex }
        };
        var point = new ChartPoint(Mock.Of<IChartView>(), series, entity);
        typeof(ChartPointContext).GetProperty(nameof(ChartPointContext.DataSource))!
            .SetValue(point.Context, series.Values!.Cast<object>().ElementAt(monthIndex));
        var month = CultureInfo.GetCultureInfo("en-US").DateTimeFormat.GetMonthName(monthIndex + 1);
        var year = seriesIndex == 0 ? _viewModel.Year : _viewModel.PreviousYear;

        var tooltip = series.GetPrimaryToolTipText(point);

        tooltip.Should().Be($"{month} {year}: {expectedVolume}");
        _localizationService.Verify(service => service.GetMonthName(monthIndex), Times.AtLeastOnce);
    }

    [Test]
    public async Task DisplayOptions_ShouldRebuildSeriesLimitAndAxisLabels()
    {
        await _viewModel.PreviousPeriodCommand.Execute().ToTask();

        _viewModel.ShowPreviousPeriod = true;
        _viewModel.ShowLimit = true;

        _viewModel.Series.Should().HaveCount(2);
        _viewModel.Sections.Should().ContainSingle();
        _viewModel.Sections![0].Yi.Should().BeApproximately(4, 0.000001);
        _viewModel.Sections[0].Yj.Should().Be(_viewModel.Sections[0].Yi);
        _viewModel.YAxes![0].Labeler(0).Should().BeEmpty();
        _viewModel.YAxes[0].Labeler(4).Should().Be("1 KB");

        _viewModel.UseProgressiveScale = false;

        _viewModel.Sections![0].Yi.Should().Be(1024);
        _viewModel.YAxes![0].Labeler(1024).Should().Be("1 KB");
        _viewModel.YAxes[0].MaxLimit.Should().BeApproximately(1048576 * 1.1, 0.000001);

        _viewModel.ShowPreviousPeriod = false;
        _viewModel.ShowLimit = false;

        _viewModel.Series.Should().ContainSingle();
        _viewModel.Sections.Should().BeEmpty();
    }

    [Test]
    public async Task NextPeriod_ShouldReloadCurrentYearAfterNavigatingBack()
    {
        var currentYear = _viewModel.Year;
        await _viewModel.PreviousPeriodCommand.Execute().ToTask();

        await _viewModel.NextPeriodCommand.Execute().ToTask();

        _viewModel.Year.Should().Be(currentYear);
        _viewModel.PreviousYear.Should().Be(currentYear - 1);
        _statisticsService.Verify(service => service.GetUsageStatistics(
            It.Is<UsageStatisticsRequest>(request => request.Year == currentYear)), Times.Once);
    }

    private static UsageStatisticsPeriod CreatePeriod()
    {
        return new UsageStatisticsPeriod
        {
            UploadedVolumePerSubPeriod = Enumerable.Range(0, 12)
                .Select(index => new UsageStatisticsSubPeriod($"Month {index}", index == 11 ? 1048576 : 0))
                .ToList()
        };
    }
}
