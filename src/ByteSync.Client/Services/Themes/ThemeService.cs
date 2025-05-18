using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia;
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
        
        _selectedTheme = new BehaviorSubject<Theme>(new Theme(
            "undefined", 
            ThemeModes.Light,
            new ThemeColor("#094177"), 
            new ThemeColor("#b88746")));
    }
    
    public IObservable<Theme> SelectedTheme => _selectedTheme.AsObservable();

    public List<Theme> AvailableThemes { get; }

    public void OnThemesRegistred()
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
        if (Application.Current?.Styles.OfType<FluentTheme>().FirstOrDefault() is FluentTheme fluentTheme)
        {
            try
            {
                // Apply theme colors to fluent theme resources
                ApplyThemeColorsToFluentTheme(fluentTheme, theme);

                // Apply theme variant (light/dark)
                Application.Current.RequestedThemeVariant = theme.IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply theme {ThemeName}", theme.Key);
            }
        }
        
        _selectedTheme.OnNext(theme);
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
        Application.Current?.Styles.TryGetResource(resourceName, themeVariant, out styleResource);

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