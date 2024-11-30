using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using ByteSync.Assets.Resources;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Services.Converters;
using ByteSync.ViewModels.Misc;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SkiaSharp;

namespace ByteSync.ViewModels.AccountDetails;

public class UsageStatisticsViewModel : FlyoutElementViewModel
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILocalizationService _localizationService;
    private readonly IThemeService _themeService;
    
    private const double PROGRESSIVE_MODE_POWER_BASE = 0.2d;
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;

    public UsageStatisticsViewModel()
    {
        
    }

    public UsageStatisticsViewModel(IStatisticsService storageDataRetriever,
        ILocalizationService localizationService, IThemeService themeManager, 
        IApplicationSettingsRepository applicationSettingsManager)
    {
        _statisticsService = storageDataRetriever;
        _localizationService = localizationService;
        _themeService = themeManager;
        _applicationSettingsRepository = applicationSettingsManager;
        
        UseProgressiveScale = true;
        Year = DateTime.Now.Year;

        Series = [];
        XAxes = [];
        YAxes = [];
        Sections = [];
        
    #if DEBUG
        if (Design.IsDesignMode)
        {
            return;
        }
    #endif
        
        var canGoPreviousPeriod = this.WhenAnyValue(x => x.Year, (year) => year > DateTime.Now.Year - 5);
        PreviousPeriodCommand = ReactiveCommand.CreateFromTask(PreviousPeriod, canGoPreviousPeriod);
        
        var canGoNextPeriod = this.WhenAnyValue(x => x.Year, (year) => year < DateTime.Now.Year);
        NextPeriodCommand = ReactiveCommand.CreateFromTask(NextPeriod, canGoNextPeriod);

        this.WhenActivated(HandleActivation);
    }

    private UsageStatisticsData UsageStatisticsData { get; set; } = null!;

    [Reactive]
    public bool UseProgressiveScale { get; set; }
    
    [Reactive]
    public ISeries[]? Series { get; set; }
    
    [Reactive]
    public Axis[]? XAxes { get; set; }
    
    [Reactive]
    public Axis[]? YAxes { get; set; }
    
    [Reactive]
    public RectangularSection[]? Sections { get; set; }
    
    [Reactive]
    public int Year { get; set; }
    
    [Reactive]
    public int PreviousYear { get; set; }
    
    [Reactive]
    public bool ShowPreviousPeriod { get; set; }
    
    [Reactive]
    public bool ShowLimit { get; set; }
    
    public ReactiveCommand<Unit, Unit> PreviousPeriodCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> NextPeriodCommand { get; set; }
    
    private async void HandleActivation(Action<IDisposable> disposables)
    {
        this.WhenAnyValue(x => x.ShowPreviousPeriod)
            .Skip(1)
            .Subscribe(_ => ShowData());
        
        this.WhenAnyValue(x => x.UseProgressiveScale)
            .Skip(1)
            .Subscribe(_ => ShowData());
        
        this.WhenAnyValue(x => x.ShowLimit)
            .Skip(1)
            .Subscribe(_ =>
            {
                ShowData();
            });

        this.WhenAnyValue(x => x.Year)
            .Subscribe(_ => PreviousYear = Year - 1);
        
        // commented on PR#23
        // await LoadAndShowData(true);
    }

    private async Task LoadAndShowData(bool isInitial)
    {
        var usageStatisticsRequest = new UsageStatisticsRequest { Year = Year };
        
        UsageStatisticsData = await _statisticsService.GetUsageStatistics(usageStatisticsRequest);

        if (isInitial)
        {
            ResetUseProgressiveMode();
        }
        
        ShowData();
    }

    private void ResetUseProgressiveMode()
    {
        // On détermine ici si on applique le mode progressif du graphique qui permet de lisser les écarts
        var maxValue = GetMaxValue();

        var allSubPeriods = new List<UsageStatisticsSubPeriod>();
        allSubPeriods.AddAll(UsageStatisticsData.CurrentPeriodData.UploadedVolumePerSubPeriod);
        allSubPeriods.AddAll(UsageStatisticsData.PreviousPeriodData.UploadedVolumePerSubPeriod);
            
        UseProgressiveScale = allSubPeriods.Any(v => v.UploadedVolume != 0 && maxValue / v.UploadedVolume > 10000);
    }

    private void ShowData()
    {
        ResetXAxes();
        ResetYAxes();
        ResetSeries();
        ResetSections();
    }

    private void ResetXAxes()
    {
        var axis = new Axis();
        axis.Labels = new List<string>();

        // On affiche les initiales de tous les mois, de janvier à décembre
        for (var i = 0; i < 12; i++)
        {
            axis.Labels.Add(_localizationService.GetMonthName(i)[..1].ToUpper());
        }
        axis.MinStep = 1;
        axis.ForceStepToMin = true;
        axis.MinLimit = -1;
        axis.MaxLimit = 12;
        
        SolidColorBrush? mainForeColorBrush;
        _themeService.GetResource("SystemControlForegroundBaseHighBrush", out mainForeColorBrush);
        if (mainForeColorBrush != null)
        {
            axis.LabelsPaint = new SolidColorPaint(new SKColor(mainForeColorBrush.Color.R, mainForeColorBrush.Color.G, mainForeColorBrush.Color.B));
        }
        axis.TextSize = 14;
        
        XAxes = [axis];
    }

    private void ResetYAxes()
    {
        var yAxis = new Axis
        {
            MinLimit = 0,

            // Gestion des labels en fonction du mode
            Labeler = (value) =>
            {
                if (value == 0)
                {
                    return "";
                }

                var applicablePrintableValue = UseProgressiveScale ? Math.Pow(value, 1 / PROGRESSIVE_MODE_POWER_BASE) : value;
                return (string)new FormatKbSizeConverter().Convert(applicablePrintableValue, typeof(string),
                    new FormatKbSizeConverterParameters { Format = "N0", ConvertUntilTeraBytes = true }, null);
            }
        };

        // maxValue : valeur max à afficher (entre la limite et la valeur de période max)
        // applicableMaxValue : maxValue, éventuellement ramenée à la valeur proressive
        // minStep : on démarre d'une valeur différente en ProgressiveMode et en AbsoluteMode
        var maxValue = GetMaxValue();
        double applicableMaxValue;
        long minStep;
        int minStepWhileMultiplicator;
        if (UseProgressiveScale)
        {
            applicableMaxValue = Math.Pow(maxValue, PROGRESSIVE_MODE_POWER_BASE);
            minStep = 2;
            minStepWhileMultiplicator = 8;
        }
        else
        {
            applicableMaxValue = maxValue;
            minStep = 2;
            minStepWhileMultiplicator = 4;
        }

        // Algorithme qui permet de déterminer la valeur finale de minStep, et la limite haute du graphique
        while (minStep * minStepWhileMultiplicator < applicableMaxValue)
        {
            minStep *= 2;
        }
        yAxis.MinStep = minStep;
        yAxis.ForceStepToMin = true;
        yAxis.MaxLimit = applicableMaxValue * 1.1;

        SolidColorBrush? mainForeColorBrush;
        _themeService.GetResource("SystemControlForegroundBaseHighBrush", out mainForeColorBrush);
        if (mainForeColorBrush != null)
        {
            yAxis.LabelsPaint = new SolidColorPaint(new SKColor(mainForeColorBrush.Color.R, mainForeColorBrush.Color.G, mainForeColorBrush.Color.B));
        }
        yAxis.TextSize = 14;

        YAxes =
        [
            yAxis
        ];
    }
    
    private void ResetSeries()
    {
        var series = new List<ISeries>();
        
        var currentPeriodSerie = BuildCurrentPeriodSerie();
        series.Add(currentPeriodSerie);

        if (ShowPreviousPeriod)
        {
            var previousPeriodSerie = BuildPreviousPeriodSerie();
            series.Add(previousPeriodSerie);
        }

        Series = series.ToArray();
    }

    private void ResetSections()
    {
        if (ShowLimit)
        {
            double limit = _applicationSettingsRepository.ProductSerialDescription!.AllowedCloudSynchronizationVolumeInBytes;
            if (UseProgressiveScale)
            {
                limit = Math.Pow(limit, PROGRESSIVE_MODE_POWER_BASE);
            }
        
            var limitSection = new RectangularSection
            {
                Yi = limit,
                Yj = limit,
                Stroke = new SolidColorPaint
                {
                    Color = SKColors.Orange,
                    StrokeThickness = 3,
                    PathEffect = new DashEffect([6, 6])
                },
            };

            Sections = [limitSection];
        }
        else
        {
            Sections = [];
        }
    }

    private ColumnSeries<LogarithmicPoint> BuildCurrentPeriodSerie()
    {
        Color chartsMainBarColor;
        _themeService.GetResource("ChartsMainBarColor", out chartsMainBarColor);
        
        Color chartsAlternateBarColor;
        _themeService.GetResource("ChartsAlternateBarColor", out chartsAlternateBarColor);

        var mainColumnSerie = new ColumnSeries<LogarithmicPoint>
        {
            Values = BuildValues(UsageStatisticsData.CurrentPeriodData),
            TooltipLabelFormatter = BuildToolTipLabel,
            Fill = new SolidColorPaint(new SKColor(chartsMainBarColor.R, chartsMainBarColor.G, chartsMainBarColor.B)),
            Mapping = BuildMapping
        };

        // 28/01/2023 : Pour l'instant, on annule ce comportement qui n'est pas forcément utile car le mois en cours est toujours le dernier
        // if (Year == DateTime.Now.Year)
        // {
        //     // https://github.com/beto-rodriguez/LiveCharts2/issues/229
        //
        //     // On peint le mois actif en couleur secondaire pour le faire ressortir
        //     var currentMonthColor = new SKColor(chartsAlternateBarColor.R, chartsAlternateBarColor.G, chartsAlternateBarColor.B);
        //     mainColumnSerie.WithConditionalPaint(new SolidColorPaint(currentMonthColor))
        //         .When(point => point.Context.Entity.EntityIndex == DateTime.Now.Month - 1);
        // }

        return mainColumnSerie;
    }

    private ISeries BuildPreviousPeriodSerie()
    {
        Color chartsMainLineColor;
        _themeService.GetResource("ChartsMainLineColor", out chartsMainLineColor);

        var lineSeries = new LineSeries<LogarithmicPoint>
        {
            Values = BuildValues(UsageStatisticsData.PreviousPeriodData),
            TooltipLabelFormatter = BuildToolTipLabel,
            Stroke = new SolidColorPaint(new SKColor(chartsMainLineColor.R, chartsMainLineColor.G, chartsMainLineColor.B), 3),
            GeometrySize = 0,
            GeometryStroke = null,
            GeometryFill = null,
            Fill = null,
            LineSmoothness = 0,
            Mapping = BuildMapping
        };

        return lineSeries;
    }

    private List<LogarithmicPoint> BuildValues(UsageStatisticsPeriod usageStatisticsPeriod)
    {
        var values = new List<LogarithmicPoint>();
        
        var cpt = 0;
        foreach (var subPeriod in usageStatisticsPeriod.UploadedVolumePerSubPeriod)
        {
            values.Add(new LogarithmicPoint(cpt, subPeriod.UploadedVolume));
            cpt += 1;
        }

        return values;
    }

    private string BuildToolTipLabel(ChartPoint<LogarithmicPoint, BezierPoint<CircleGeometry>, LabelGeometry> chartPoint)
    {
        return DoBuildToolTipLabel(chartPoint.Context, chartPoint.Model!, PreviousYear);
    }

    private string BuildToolTipLabel(ChartPoint<LogarithmicPoint, RoundedRectangleGeometry, LabelGeometry> chartPoint)
    {
        return DoBuildToolTipLabel(chartPoint.Context, chartPoint.Model!, Year);
    }

    private string DoBuildToolTipLabel(ChartPointContext context, LogarithmicPoint model, int year)
    {
        // Exemple : "Janvier 2023 - 520 987 o (507,29 Ko)
        var monthName = _localizationService.GetMonthName(context.Entity.EntityIndex);
        var result = $"{string.Format(Resources.General_MonthYearColon, monthName, year)} ";
                    
        if (model.Volume > 1024)
        {
            result += new FormatKbSizeConverter().Convert(model.Volume, typeof(string),
                          new FormatKbSizeConverterParameters { Format = "N2", ConvertUntilTeraBytes = true }, null) +
                      " (" +  
                      $"{model.Volume:N0} {_localizationService[nameof(Resources.Misc_SizeUnit_Byte)]}" +
                      ")";
        }
        else
        {
            result += $"{model.Volume:N0} {_localizationService[nameof(Resources.Misc_SizeUnit_Byte)]}";
        }

        return result;
    }

    private void BuildMapping(LogarithmicPoint logPoint, ChartPoint chartPoint)
    {
        // Le mapping permet d'adapter la valeur quand le mode progressif est utilisé
                
        // https://lvcharts.com/docs/Avalonia/2.0.0-beta.700/samples.axes.logarithmic

        // for the x coordinate, we use the X property of the LogaritmicPoint instance
        chartPoint.SecondaryValue = logPoint.X;

        // but for the Y coordinate, we will map to the logarithm of the value
        if (UseProgressiveScale)
        {
            chartPoint.PrimaryValue = Math.Pow(logPoint.Volume, PROGRESSIVE_MODE_POWER_BASE);
        }
        else
        {
            chartPoint.PrimaryValue = logPoint.Volume;
        }
    }

    private class LogarithmicPoint
    {
        public LogarithmicPoint(int x, long volume)
        {
            X = x;
            Volume = volume;
        }
        
        public int X { get; }
        
        public long Volume { get; }
    }
    
    private async Task PreviousPeriod()
    {
        Year = Year - 1;
        
        await LoadAndShowData(false);
    }

    private async Task NextPeriod()
    {
        Year = Year + 1;
        
        await LoadAndShowData(false);
    }
    
    private long GetMaxValue()
    {
        long maxValue;
        
        if (ShowLimit)
        {
            maxValue = Math.Max(UsageStatisticsData.GetMaxTransferedVolume(), 
                _applicationSettingsRepository.ProductSerialDescription!.AllowedCloudSynchronizationVolumeInBytes);
        }
        else
        {
            maxValue = UsageStatisticsData.GetMaxTransferedVolume();
        }

        return maxValue;
    }
}