using Autofac;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using ByteSync.Business.Themes;
using ByteSync.Interfaces.Factories;
using ByteSync.Services;
using ByteSync.Services.Themes;
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
            
            LiveCharts.Configure(config =>
                config
                    .AddSkiaSharp()
                    .AddDefaultMappers()
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
            var themeBuilder = new ThemeBuilder();
            var defaultTheme = themeBuilder.BuildDefaultTheme();
            
            var customThemeResources = new ResourceDictionary();
            Current!.Resources.MergedDictionaries.Add(customThemeResources);
            
            Current!.RequestedThemeVariant = defaultTheme.IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
            
            customThemeResources["SystemAccentColor"] = defaultTheme.ThemeColor.AvaloniaColor;
            ApplyColorSchemeProperties(customThemeResources, defaultTheme.ColorScheme);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load design mode resources: {ex}");
        }
    }
    
    private static void ApplyColorSchemeProperties(ResourceDictionary resourceDictionary, ColorScheme colorScheme)
    {
        var properties = typeof(ColorScheme).GetProperties();
        
        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(colorScheme);
                
                if (value == null) continue;
                
                if (property.PropertyType == typeof(Color))
                {
                    resourceDictionary[property.Name] = (Color)value;
                }
                else if (property.PropertyType == typeof(ThemeColor))
                {
                    resourceDictionary[property.Name] = ((ThemeColor)value).AvaloniaColor;
                }
                else if (property.PropertyType == typeof(SolidColorBrush))
                {
                    resourceDictionary[property.Name] = (SolidColorBrush)value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to apply color scheme property: {property.Name} - {ex.Message}");
            }
        }
    }
}