using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using ByteSync.Business.Themes;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Themes;

namespace ByteSync.Services.Themes;

class ThemeService : IThemeService
{
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;
    private readonly ILogger<ThemeService> _logger;
    
    private readonly BehaviorSubject<Theme> _selectedTheme;

    public ThemeService(IApplicationSettingsRepository applicationSettingsRepository, ILogger<ThemeService> logger)
    {
        _applicationSettingsRepository = applicationSettingsRepository;
        _logger = logger;

        AvailableThemes = new List<Theme>();
        
        _selectedTheme = new BehaviorSubject<Theme>(new Theme("undefined", ThemeModes.Light, null!, 
            new ThemeColor("#094177"), new ThemeColor("#b88746")));
    }
    
    public IObservable<Theme> SelectedTheme => _selectedTheme.AsObservable();

    public List<Theme> AvailableThemes { get; }

    public void OnThemesRegistred()
    {
        Theme? theme = null;
        
        // var fluentThemes = Application.Current?.Styles.Where(s => s is Avalonia.Themes.Fluent.FluentTheme).ToList();
        // if (fluentThemes != null)
        // {
        //     foreach (var fluentTheme in fluentThemes)
        //     {
        //         Application.Current!.Styles.Remove(fluentTheme);
        //     }
        // }
        
        var applicationSettings = _applicationSettingsRepository.GetCurrentApplicationSettings();
        if (applicationSettings.Theme.IsNotEmpty())
        {
            theme = AvailableThemes.FirstOrDefault(t => t.Key.Equals(applicationSettings.Theme, StringComparison.CurrentCultureIgnoreCase));
        }
        
        if (theme == null)
        {
            UseDefaultTheme();
            
            UpdateSettings();
        }
        else
        {
            SelectTheme(theme);
        }
    }
    
    public void SelectTheme(string? name, bool isDarkMode)
    {
        var currentTheme = _selectedTheme.Value;
        
        if (!currentTheme.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) ||
            currentTheme.IsDarkMode != isDarkMode)
        {
            var theme = AvailableThemes.Single(t => t.Name.Equals(name) && t.IsDarkMode == isDarkMode);
        
            SelectTheme(theme);
        
            UpdateSettings();
        }
    }

    public void RegisterTheme(Theme theme)
    {
        AvailableThemes.Add(theme);
    }

    private void SelectTheme(Theme theme)
    {
        if (Application.Current?.Styles.OfType<FluentTheme>().FirstOrDefault() is FluentTheme fluentTheme)
        {
            try
            {
                ChangerThemeColors(fluentTheme, theme, theme.ThemeColor, "Accent");
                ChangerThemeColors(fluentTheme, theme, theme.SecondaryThemeColor, "Secondary");

                // fluentTheme.Resources["SystemAccentColor"] = theme.ThemeColor.AvaloniaColor;

                Application.Current.RequestedThemeVariant = theme.IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;

                // var vs = Application.Current.Styles;
                // vs.Remove(fluentTheme);
                // // if (DateTime.Now.Ticks % 2 == 0)
                // // {
                // //     vs.Add(fluentTheme);
                // // }
                // vs.Add(fluentTheme);
                // // vs.Add(fluentTheme);

                // Application.Current.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
            }
            catch (Exception ex)
            {
                
            }
        
            
            // Application.Current.Set = theme.IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
            // fluentTheme.Mode
        }
        
        // var styles = Application.Current?.Styles;
        // if (styles == null)
        // {
        //     return;
        // }
        //             
        // if (styles.Count == 0 || styles.All(s => s is StyleInclude))
        // {
        //     // We haven't added the style yet, we are adding it
        //     styles.Add(theme.Style);
        // }
        // else
        // {
        //     // Otherwise, we replace
        //     styles[^1] = theme.Style;
        // }
        
        _selectedTheme.OnNext(theme);
    }
    
    public void ChangerThemeColors(FluentTheme fluentTheme, Theme theme, ThemeColor themeColor, string mainName)
    {
        // var resources = Application.Current.Resources;
    
        // Définir la couleur d'accentuation principale
        fluentTheme.Resources[$"System{mainName}Color"] = themeColor.AvaloniaColor;
    
        // Calculer automatiquement les variantes (assombrissement/éclaircissement)
        var hsl = themeColor.AvaloniaColor.ToHsl();
        
        // dark1step = (hslAccent.L - SystemAccentColorDark1.L) * 255
        const double dark1step = 28.5 / 255d;
        const double dark2step = 49 / 255d;
        const double dark3step = 74.5 / 255d;
        // light1step = (SystemAccentColorLight1.L - hslAccent.L) * 255
        const double light1step = 39 / 255d;
        const double light2step = 70 / 255d;
        const double light3step = 103 / 255d;
        
        var hslAccent =  themeColor.AvaloniaColor.ToHsl();

        // return (
        //     // Darker shades
        //     new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L - dark1step).ToRgb(),
        //     new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L - dark2step).ToRgb(),
        //     new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L - dark3step).ToRgb(),
        //
        //     // Lighter shades
        //     new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L + light1step).ToRgb(),
        //     new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L + light2step).ToRgb(),
        //     new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L + light3step).ToRgb()
        // );
        //
        // Variantes foncées (réduire la luminosité)
        var dark1 = new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L - dark1step).ToRgb();
        var dark2 = new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L - dark2step).ToRgb(); 
        var dark3 = new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L - dark3step).ToRgb();
    
        // Variantes claires (augmenter la luminosité)
        var light1 = new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L + light1step).ToRgb();
        var light2 = new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L + light2step).ToRgb();
        var light3 = new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L + light3step).ToRgb();
        

        
        var colorScheme = theme.ColorScheme;
        if (colorScheme != null)
        {
            var properties = colorScheme.GetType().GetProperties();

            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(Color))
                {
                    var color = (Color)property.GetValue(colorScheme)!;
                    fluentTheme.Resources[property.Name] = color;
                }
                else if (property.PropertyType == typeof(ThemeColor))
                {
                    var themeColorProperty = (ThemeColor)property.GetValue(colorScheme)!;
                    fluentTheme.Resources[property.Name] = themeColorProperty.AvaloniaColor;
                }
                else if (property.PropertyType == typeof(SolidColorBrush))
                {
                    var brush = (SolidColorBrush)property.GetValue(colorScheme)!;
                    fluentTheme.Resources[property.Name] = brush;
                }
                else
                {
                    
                }
            }
        }
        
        // Appliquer les variantes
        fluentTheme.Resources[$"System{mainName}ColorDark1"] = dark1;
        fluentTheme.Resources[$"System{mainName}ColorDark2"] = dark2;
        fluentTheme.Resources[$"System{mainName}ColorDark3"] = dark3;
        fluentTheme.Resources[$"System{mainName}ColorLight1"] = light1;
        fluentTheme.Resources[$"System{mainName}ColorLight2"] = light2;
        fluentTheme.Resources[$"System{mainName}ColorLight3"] = light3;
    }

    private void UseDefaultTheme()
    {
        var defaultTheme = AvailableThemes.Single(t => 
            (t.Name.Equals(ThemeConstants.BLUE) || t.Name.Equals(ThemeConstants.BLUE + "1"))
            && t.Mode == ThemeModes.Light);
        
        SelectTheme(defaultTheme);
    }
    
    private void UpdateSettings()
    {
        _applicationSettingsRepository.UpdateCurrentApplicationSettings(
            settings => settings.Theme = _selectedTheme.Value.Key);
    }

    public void GetResource<T>(string resourceName, out T? resource)
    {
        var themeVariant = Application.Current?.RequestedThemeVariant ?? ThemeVariant.Default;
        
        object? styleResource = null;
        Application.Current?.Styles.TryGetResource(resourceName, themeVariant, out styleResource);

        if (styleResource is T)
        {
            resource = (T) styleResource;
        }
        else
        {
            resource = default;
            _logger.LogWarning("Resource {ResourceName} not found", resourceName);
        }
    }
    
    public IBrush? GetBrush(string resourceName)
    {
        var themeVariant = Application.Current?.RequestedThemeVariant ?? ThemeVariant.Default;
        
        object? styleResource = null;
        Application.Current?.Styles.TryGetResource(resourceName, themeVariant,  out styleResource);

        if (styleResource is IBrush brush)
        {
            return brush;
        }
        else if (styleResource is Color color)
        {
            return new SolidColorBrush(color);
        }
        else
        {
            _logger.LogWarning("Resource {ResourceName} not found", resourceName);
            return null;
        }
    }
}