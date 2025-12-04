using Autofac;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using ByteSync.Business.Themes;
using ByteSync.Helpers;
using ByteSync.Interfaces.Factories;
using ByteSync.Services;
using ByteSync.ViewModels;
using ByteSync.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace ByteSync;

public class App : Application
{
    public override void Initialize()
    {
        try
        {
            AvaloniaXamlLoader.Load(this);
            
            // https://lvcharts.com/docs/Avalonia/2.0.0-beta.700/Overview.Installation
            LiveCharts.Configure(config =>
                config
                    
                    // registers SkiaSharp as the library backend
                    // REQUIRED unless you build your own
                    .AddSkiaSharp()
                    
                    // adds the default supported types
                    // OPTIONAL but highly recommend
                    .AddDefaultMappers()
                    
                    // select a theme, default is Light
                    // OPTIONAL
                    //.AddDarkTheme()
                    .AddLightTheme()
            );
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            
            throw;
        }
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        if (Design.IsDesignMode)
        {
            LoadDesignModeResources();
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel()
                };
            }
            
            base.OnFrameworkInitializationCompleted();
        }
        else
        {
            var container = ContainerProvider.Container;
            
            var bootstrapperFactory = container.Resolve<IBootstrapperFactory>();
            
            var bootStrapper = bootstrapperFactory.CreateBootstrapper();
            bootStrapper.AfterFrameworkInitializationCompleted();
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = container.Resolve<MainWindow>();
            }
            
            base.OnFrameworkInitializationCompleted();
        }
    }
    
    private void LoadDesignModeResources()
    {
        try
        {
            var customThemeResources = new ResourceDictionary();
            Current!.Resources.MergedDictionaries.Add(customThemeResources);
            
            var defaultTheme = CreateDefaultDesignTheme();
            Current!.RequestedThemeVariant = defaultTheme.IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
            
            customThemeResources["SystemAccentColor"] = defaultTheme.ThemeColor.AvaloniaColor;
            ApplyColorSchemeProperties(customThemeResources, defaultTheme.ColorScheme);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load design mode resources: {ex}");
        }
    }
    
    private Theme CreateDefaultDesignTheme()
    {
        var themeColor = new ThemeColor("#094177");
        var secondaryColorHue = themeColor.Hue - 60;
        var secondarySystemColor = ColorUtils.ColorFromHsv(secondaryColorHue, themeColor.Saturation, themeColor.Value);
        var secondaryThemeColor = new ThemeColor(secondarySystemColor);
        
        var theme = new Theme("Blue1", ThemeModes.Light, themeColor, secondaryThemeColor);
        theme.ColorScheme = BuildColorScheme(theme, themeColor, secondaryColorHue, ThemeModes.Light);
        
        return theme;
    }
    
    private ColorScheme BuildColorScheme(Theme theme, ThemeColor themeColor, double secondaryColorHue, ThemeModes themeMode)
    {
        var colorScheme = new ColorScheme(themeMode);
        
        if (themeMode == ThemeModes.Dark)
        {
            colorScheme.MainAccentColor = themeColor.WithSaturationValue(0.65, 0.50);
            colorScheme.MainSecondaryColor = colorScheme.MainAccentColor.WithHue(secondaryColorHue);
            
            colorScheme.AccentTextForeGround = themeColor.WithSaturationValue(0.33, 0.85).AvaloniaColor;
            
            colorScheme.HomeCloudSynchronizationBackGround = themeColor
                .WithSaturationValue(0.55, 0.70).AvaloniaColor;
            colorScheme.HomeCloudSynchronizationPointerOverBackGround = themeColor
                .WithSaturationValue(0.25, 0.50).AvaloniaColor;
            colorScheme.HomeLocalSynchronizationBackGround = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.55, 0.70).AvaloniaColor;
            colorScheme.HomeLocalSynchronizationPointerOverBackGround = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.25, 0.50).AvaloniaColor;
            
            colorScheme.ChartsMainBarColor = themeColor
                .WithSaturationValue(0.50, 0.75).AvaloniaColor;
            colorScheme.ChartsAlternateBarColor = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.50, 0.75).AvaloniaColor;
            colorScheme.ChartsMainLineColor = colorScheme.MainAccentColor.AvaloniaColor;
            
            colorScheme.CurrentMemberBackGround = themeColor
                .WithSaturationValue(0.35, 0.22);
            colorScheme.DisabledMemberBackGround = new ThemeColor(Color.FromArgb(0xFF, 0x30, 0x30, 0x30));
            colorScheme.OtherMemberBackGround = new ThemeColor(Color.FromArgb(0xFF, 0x30, 0x30, 0x30));
            
            colorScheme.ConnectedMemberLetterBackGround = themeColor
                .WithSaturationValue(0.35, 0.28);
            colorScheme.DisabledMemberLetterBackGround = new ThemeColor(Color.FromArgb(0xFF, 0x37, 0x37, 0x37));
            
            colorScheme.ConnectedMemberLetterBorder = themeColor
                .WithSaturationValue(0.35, 0.34);
            colorScheme.DisabledMemberLetterBorder = new ThemeColor(Color.FromArgb(0xFF, 0x3D, 0x3D, 0x3D));
            
            colorScheme.BsAccentButtonBackGround = themeColor
                .WithSaturationValue(0.55, 0.25).AvaloniaColor;
            colorScheme.BsAccentButtonPointerOverBackGround = themeColor
                .WithSaturationValue(0.55, 0.35).AvaloniaColor;
            
            colorScheme.SecondaryButtonBackGround = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.55, 0.25).AvaloniaColor;
            colorScheme.SecondaryButtonPointerOverBackGround = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.55, 0.35).AvaloniaColor;
            
            colorScheme.StatusMainBackGround = themeColor
                .WithSaturationValue(0.45, 0.25);
            
            ComputeAttenuations(colorScheme);
            
            colorScheme.Accent1 = colorScheme.SystemAccentColorDark1.AvaloniaColor;
            colorScheme.Accent2 = colorScheme.SystemAccentColorDark2.AvaloniaColor;
            colorScheme.Accent3 = colorScheme.SystemAccentColorDark3.AvaloniaColor;
            colorScheme.Accent4 = colorScheme.SystemAccentColorDark4.AvaloniaColor;
            colorScheme.Accent5 = colorScheme.SystemAccentColorDark5.AvaloniaColor;
            
            colorScheme.VeryLightGray = Color.FromArgb(0xFF, 0x12, 0x12, 0x12);
            colorScheme.GenericButtonBorder = Color.FromArgb(0xFF, 0x55, 0x55, 0x55);
            colorScheme.Gray1 = Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC);
            colorScheme.Gray2 = Color.FromArgb(0xFF, 0x80, 0x80, 0x80);
            colorScheme.Gray5 = Color.FromArgb(0xFF, 0x46, 0x46, 0x46);
            colorScheme.Gray7 = Color.FromArgb(0xFF, 0x3A, 0x3A, 0x3A);
            colorScheme.Gray8 = Color.FromArgb(0xFF, 0x2C, 0x2C, 0x2C);
            colorScheme.SettingsHeaderColor = Color.FromArgb(0xFF, 0x30, 0x30, 0x30);
            colorScheme.BlockBackColor = Color.FromArgb(0xFF, 0x1F, 0x1F, 0x1F);
            
            colorScheme.MainWindowTopColor = colorScheme.VeryLightGray;
            colorScheme.MainWindowBottomColor = Color.FromArgb(0xFF, 0x04, 0x04, 0x04);
            
            ComputeSecondaryColors(colorScheme, secondaryColorHue, themeMode);
            
            colorScheme.StatusMainBackGroundBrush = new SolidColorBrush(colorScheme.StatusMainBackGround.AvaloniaColor);
            colorScheme.StatusSecondaryBackGroundBrush = new SolidColorBrush(colorScheme.StatusSecondaryBackGround.AvaloniaColor);
            colorScheme.VeryLightGrayBrush = new SolidColorBrush(colorScheme.VeryLightGray);
            
            colorScheme.TextControlSelectionHighlightColor = colorScheme.BsAccentButtonBackGround;
        }
        else
        {
            colorScheme.MainAccentColor = themeColor;
            colorScheme.MainSecondaryColor = colorScheme.MainAccentColor.WithHue(secondaryColorHue);
            
            colorScheme.AccentTextForeGround = themeColor.AvaloniaColor;
            
            colorScheme.HomeCloudSynchronizationBackGround = themeColor
                .WithSaturationValue(0.50, 0.65).AvaloniaColor;
            colorScheme.HomeCloudSynchronizationPointerOverBackGround = themeColor
                .WithSaturationValue(0.25, 0.55).AvaloniaColor;
            colorScheme.HomeLocalSynchronizationBackGround = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.50, 0.65).AvaloniaColor;
            colorScheme.HomeLocalSynchronizationPointerOverBackGround = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.25, 0.55).AvaloniaColor;
            
            colorScheme.ChartsMainBarColor = themeColor
                .WithSaturationValue(0.50, 0.80).AvaloniaColor;
            colorScheme.ChartsAlternateBarColor = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.50, 0.80).AvaloniaColor;
            colorScheme.ChartsMainLineColor = colorScheme.MainAccentColor.AvaloniaColor;
            
            colorScheme.CurrentMemberBackGround = themeColor
                .WithSaturationValue(0.20, 0.92);
            colorScheme.DisabledMemberBackGround = new ThemeColor(Color.FromArgb(0xFF, 0xEC, 0xEC, 0xEC));
            colorScheme.OtherMemberBackGround = new ThemeColor(Color.FromArgb(0xFF, 0xEC, 0xEC, 0xEC));
            
            colorScheme.ConnectedMemberLetterBackGround = themeColor
                .WithSaturationValue(0.20, 0.84);
            colorScheme.DisabledMemberLetterBackGround = new ThemeColor(Color.FromArgb(0xFF, 0xE6, 0xE6, 0xE6));
            
            colorScheme.ConnectedMemberLetterBorder = themeColor
                .WithSaturationValue(0.20, 0.78);
            colorScheme.DisabledMemberLetterBorder = new ThemeColor(Color.FromArgb(0xFF, 0xE0, 0xE0, 0xE0));
            
            colorScheme.BsAccentButtonBackGround = themeColor
                .WithSaturationValue(0.15, 0.95).AvaloniaColor;
            colorScheme.BsAccentButtonPointerOverBackGround = themeColor
                .WithSaturationValue(0.12, 0.98).AvaloniaColor;
            
            colorScheme.SecondaryButtonBackGround = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.15, 0.95).AvaloniaColor;
            colorScheme.SecondaryButtonPointerOverBackGround = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.12, 0.98).AvaloniaColor;
            
            colorScheme.StatusMainBackGround = themeColor
                .WithSaturationValue(0.35, 0.90);
            
            ComputeAttenuations(colorScheme);
            
            colorScheme.Accent1 = colorScheme.SystemAccentColorLight1.AvaloniaColor;
            colorScheme.Accent2 = colorScheme.SystemAccentColorLight2.AvaloniaColor;
            colorScheme.Accent3 = colorScheme.SystemAccentColorLight3.AvaloniaColor;
            colorScheme.Accent4 = colorScheme.SystemAccentColorLight4.AvaloniaColor;
            colorScheme.Accent5 = colorScheme.SystemAccentColorLight5.AvaloniaColor;
            
            colorScheme.VeryLightGray = Color.FromArgb(0xFF, 0xF7, 0xF7, 0xF7);
            colorScheme.GenericButtonBorder = Color.FromArgb(0xFF, 0xAA, 0xAA, 0xAA);
            colorScheme.Gray1 = Color.FromArgb(0xFF, 0x33, 0x33, 0x33);
            colorScheme.Gray2 = Color.FromArgb(0xFF, 0x7F, 0x7F, 0x7F);
            colorScheme.Gray5 = Color.FromArgb(0xFF, 0xB9, 0xB9, 0xB9);
            colorScheme.Gray7 = Color.FromArgb(0xFF, 0xD6, 0xD6, 0xD6);
            colorScheme.Gray8 = Color.FromArgb(0xFF, 0xE0, 0xE0, 0xE0);
            colorScheme.SettingsHeaderColor = Color.FromArgb(0xFF, 0xEC, 0xEC, 0xEC);
            colorScheme.BlockBackColor = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            
            colorScheme.MainWindowTopColor = Color.FromArgb(0xFF, 0xFA, 0xFA, 0xFA);
            colorScheme.MainWindowBottomColor = Color.FromArgb(0xFF, 0xEF, 0xEF, 0xEF);
            
            ComputeSecondaryColors(colorScheme, secondaryColorHue, themeMode);
            
            colorScheme.StatusMainBackGroundBrush = new SolidColorBrush(colorScheme.StatusMainBackGround.AvaloniaColor);
            colorScheme.StatusSecondaryBackGroundBrush = new SolidColorBrush(colorScheme.StatusSecondaryBackGround.AvaloniaColor);
            colorScheme.VeryLightGrayBrush = new SolidColorBrush(colorScheme.VeryLightGray);
            
            colorScheme.TextControlSelectionHighlightColor = colorScheme.BsAccentButtonBackGround;
        }
        
        return colorScheme;
    }
    
    private void ComputeSecondaryColors(ColorScheme colorScheme, double secondaryColorHue, ThemeModes themeMode)
    {
        for (int i = 1; i <= 3; i++)
        {
            double basePercent = -0.10;
            var secondary = colorScheme.MainSecondaryColor.AdjustSaturationValue(0, basePercent * i);
            colorScheme.SecondaryColors.Add(secondary);
        }
        
        colorScheme.CurrentMemberSecondaryBackGround = colorScheme.CurrentMemberBackGround
            .WithHue(secondaryColorHue);
        
        colorScheme.OtherMemberSecondaryBackGround = colorScheme.OtherMemberBackGround
            .WithHue(secondaryColorHue);
        
        if (themeMode == ThemeModes.Dark)
        {
            colorScheme.StatusSecondaryBackGround = colorScheme.StatusMainBackGround
                .WithHue(secondaryColorHue)
                .AdjustSaturationValue(0, +0.20);
        }
        else
        {
            colorScheme.StatusSecondaryBackGround = colorScheme.StatusMainBackGround
                .WithHue(secondaryColorHue);
        }
    }
    
    private static void ComputeAttenuations(ColorScheme colorScheme)
    {
        colorScheme.SystemAccentColorDark1 = colorScheme.MainAccentColor.AdjustSaturationValue(0, -0.10);
        colorScheme.SystemAccentColorDark2 = colorScheme.MainAccentColor.AdjustSaturationValue(0, -0.20);
        colorScheme.SystemAccentColorDark3 = colorScheme.MainAccentColor.AdjustSaturationValue(0, -0.30);
        colorScheme.SystemAccentColorDark4 = colorScheme.MainAccentColor.AdjustSaturationValue(0, -0.40);
        colorScheme.SystemAccentColorDark5 = colorScheme.MainAccentColor.AdjustSaturationValue(0, -0.50);
        
        colorScheme.SystemAccentColorLight1 = colorScheme.MainAccentColor.AdjustSaturationValue(0, 0.10);
        colorScheme.SystemAccentColorLight2 = colorScheme.MainAccentColor.AdjustSaturationValue(0, 0.20);
        colorScheme.SystemAccentColorLight3 = colorScheme.MainAccentColor.AdjustSaturationValue(0, 0.30);
        colorScheme.SystemAccentColorLight4 = colorScheme.MainAccentColor.AdjustSaturationValue(0, 0.40);
        colorScheme.SystemAccentColorLight5 = colorScheme.MainAccentColor.AdjustSaturationValue(0, 0.50);
    }
    
    private void ApplyColorSchemeProperties(ResourceDictionary resourceDictionary, object colorScheme)
    {
        var properties = colorScheme.GetType().GetProperties();
        
        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(colorScheme);
                
                if (value == null) continue;
                
                if (property.PropertyType == typeof(Color))
                {
                    var color = (Color)value;
                    resourceDictionary[property.Name] = color;
                }
                else if (property.PropertyType == typeof(ThemeColor))
                {
                    var themeColorProperty = (ThemeColor)value;
                    resourceDictionary[property.Name] = themeColorProperty.AvaloniaColor;
                }
                else if (property.PropertyType == typeof(SolidColorBrush))
                {
                    var brush = (SolidColorBrush)value;
                    resourceDictionary[property.Name] = brush;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to apply color scheme property: {property.Name} - {ex.Message}");
            }
        }
    }
}