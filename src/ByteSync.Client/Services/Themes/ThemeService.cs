using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia;
using Avalonia.Controls;
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
    private ResourceDictionary? _customThemeResources;

    public ThemeService(IApplicationSettingsRepository applicationSettingsRepository, ILogger<ThemeService> logger)
    {
        _applicationSettingsRepository = applicationSettingsRepository;
        _logger = logger;

        AvailableThemes = new List<Theme>();
        
        _selectedTheme = new BehaviorSubject<Theme>(new Theme(
            "undefined", 
            ThemeModes.Light,
            new ThemeColor("#094177"), 
            new ThemeColor("#b88746")));
    }
    
    public IObservable<Theme> SelectedTheme => _selectedTheme.AsObservable();

    public List<Theme> AvailableThemes { get; }

    public void OnThemesRegistered()
    {
        Theme? theme = null;
        
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
        if (_customThemeResources == null)
        {
            _customThemeResources = new ResourceDictionary();
            Application.Current!.Resources.MergedDictionaries.Add(_customThemeResources);
        }
        
        if (Application.Current?.Styles.OfType<FluentTheme>().FirstOrDefault() is FluentTheme fluentTheme)
        {
            try
            {
                // 1. D'abord changer le variant (light/dark)
                Application.Current.RequestedThemeVariant = theme.IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
                
                // 2. Ensuite appliquer les couleurs personnalisées
                ApplyThemeColorsToCustomResources(theme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply theme {ThemeName}", theme.Key);
            }
        }
        
        _selectedTheme.OnNext(theme);
        
        // _selectedTheme.OnNext(theme);
    }
    
    private void ApplyThemeColorsToCustomResources(Theme theme)
    {
        // NETTOYER complètement le dictionnaire à chaque changement
        _customThemeResources.Clear();
        
        _logger.LogDebug("Applying theme colors for theme: {ThemeName}, IsDark: {IsDark}", theme.Name, theme.IsDarkMode);
        
        // Appliquer la couleur principale
        _customThemeResources["SystemAccentColor"] = theme.ThemeColor.AvaloniaColor;
        // _customThemeResources["TextControlSelectionHighlightColor"] = new SolidColorBrush(theme.ThemeColor.AvaloniaColor);
        
        // Appliquer d'autres couleurs liées à l'accent qui pourraient être affectées
        // ApplyAccentRelatedColors(theme.ThemeColor.AvaloniaColor);
        
        // Apply all color scheme properties
        var colorScheme = theme.ColorScheme;
        ApplyColorSchemeProperties(colorScheme);
        
        _logger.LogDebug("Applied {Count} custom theme resources", _customThemeResources.Count);
    }
    
    // private void ApplyAccentRelatedColors(Color accentColor)
    // {
    //     // Appliquer la couleur d'accent à toutes les ressources liées
    //     _customThemeResources["SystemControlHighlightAccentBrush"] = new SolidColorBrush(accentColor);
    //     _customThemeResources["SystemControlForegroundAccentBrush"] = new SolidColorBrush(accentColor);
    //     _customThemeResources["SystemControlHighlightAccentRevealBackgroundBrush"] = new SolidColorBrush(accentColor);
    //     _customThemeResources["SystemControlHyperlinkTextBrush"] = new SolidColorBrush(accentColor);
    //     
    //     // Variations avec opacité pour les états hover/pressed
    //     _customThemeResources["SystemControlHighlightListAccentLowBrush"] = new SolidColorBrush(accentColor) { Opacity = 0.4 };
    //     _customThemeResources["SystemControlHighlightListAccentMediumBrush"] = new SolidColorBrush(accentColor) { Opacity = 0.6 };
    //     _customThemeResources["SystemControlHighlightListAccentHighBrush"] = new SolidColorBrush(accentColor) { Opacity = 0.7 };
    // }
    
    private void ApplyColorSchemeProperties(object colorScheme)
    {
        var properties = colorScheme.GetType().GetProperties();

        foreach (var property in properties)
        {
            // if (property.Name.Equals("CurrentMemberBackGround", StringComparison.OrdinalIgnoreCase) ||
            //     property.Name.Equals("OtherMemberBackGround", StringComparison.OrdinalIgnoreCase) ||
            //     property.Name.Equals("DisabledMemberBackGround", StringComparison.OrdinalIgnoreCase))
            // {
            //     // // Skip these properties as they are handled separately
            //     // continue;
            // }

            if (property.Name.Equals("SystemControlHighlightAccentBrush", StringComparison.OrdinalIgnoreCase))
            {
                
            }
            
            try
            {
                var value = property.GetValue(colorScheme);
                if (value == null) continue;

                if (property.PropertyType == typeof(Color))
                {
                    var color = (Color)value;
                    _customThemeResources[property.Name] = color;
                    _logger.LogDebug("Applied color property: {PropertyName} = {Color}", property.Name, color);
                }
                else if (property.PropertyType == typeof(ThemeColor))
                {
                    var themeColorProperty = (ThemeColor)value;
                    _customThemeResources[property.Name] = themeColorProperty.AvaloniaColor;
                    _logger.LogDebug("Applied ThemeColor property: {PropertyName} = {Color}", property.Name, themeColorProperty.AvaloniaColor);
                }
                else if (property.PropertyType == typeof(SolidColorBrush))
                {
                    var brush = (SolidColorBrush)value;
                    _customThemeResources[property.Name] = brush;
                    _logger.LogDebug("Applied brush property: {PropertyName}", property.Name);
                }
                else
                {
                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply color scheme property: {PropertyName}", property.Name);
            }
        }
    }
    
    private void ApplyThemeColorsToFluentTheme(FluentTheme fluentTheme, Theme theme)
    {
        // Set primary accent color
        fluentTheme.Resources["SystemAccentColor"] = theme.ThemeColor.AvaloniaColor;
        
        // Apply all color scheme properties to resources
        var colorScheme = theme.ColorScheme;
        if (colorScheme != null)
        {
            var properties = colorScheme.GetType().GetProperties();

            foreach (var property in properties)
            {
                // if (property.Name.Equals("CurrentMemberBackGround", StringComparison.OrdinalIgnoreCase) ||
                //     property.Name.Equals("OtherMemberBackGround", StringComparison.OrdinalIgnoreCase) ||
                //     property.Name.Equals("DisabledMemberBackGround", StringComparison.OrdinalIgnoreCase))
                // {
                //     // // Skip these properties as they are handled separately
                //     // continue;
                // }
                
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

    // public void GetResource<T>(string resourceName, out T? resource)
    // {
    //     var themeVariant = Application.Current?.RequestedThemeVariant ?? ThemeVariant.Default;
    //     
    //     object? styleResource = null;
    //     Application.Current?.Styles.TryGetResource(resourceName, themeVariant, out styleResource);
    //
    //     if (styleResource == null)
    //     {
    //         Application.Current?.Styles.TryGetResource(resourceName, ThemeVariant.Default, out styleResource);
    //     }
    //     
    //     if (styleResource is T)
    //     {
    //         resource = (T) styleResource;
    //     }
    //     else
    //     {
    //         resource = default;
    //         _logger.LogWarning("Resource {ResourceName} not found", resourceName);
    //     }
    // }
    
    public IBrush? GetBrush(string resourceName)
    {
        var themeVariant = Application.Current?.RequestedThemeVariant ?? ThemeVariant.Default;
        
        object? styleResource = null;
        Application.Current?.Styles.TryGetResource(resourceName, themeVariant, out styleResource);

        if (styleResource == null)
        {
            Application.Current?.Styles.TryGetResource(resourceName, ThemeVariant.Default, out styleResource);
        }

        if (styleResource == null)
        {
            _customThemeResources?.TryGetResource(resourceName, ThemeVariant.Default, out styleResource);
        }

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