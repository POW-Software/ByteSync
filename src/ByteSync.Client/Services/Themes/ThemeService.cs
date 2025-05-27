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
        
        try
        {
            Application.Current!.RequestedThemeVariant = theme.IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
                
            ApplyThemeColorsToCustomResources(theme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply theme {ThemeName}", theme.Key);
        }
        
        _selectedTheme.OnNext(theme);
    }
    
    private void ApplyThemeColorsToCustomResources(Theme theme)
    {
        _customThemeResources!.Clear();
        
        _customThemeResources["SystemAccentColor"] = theme.ThemeColor.AvaloniaColor;
        
        ApplyColorSchemeProperties(theme.ColorScheme);
    }
    
    private void ApplyColorSchemeProperties(object colorScheme)
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
                    _customThemeResources![property.Name] = color;
                }
                else if (property.PropertyType == typeof(ThemeColor))
                {
                    var themeColorProperty = (ThemeColor)value;
                    _customThemeResources![property.Name] = themeColorProperty.AvaloniaColor;
                }
                else if (property.PropertyType == typeof(SolidColorBrush))
                {
                    var brush = (SolidColorBrush)value;
                    _customThemeResources![property.Name] = brush;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply color scheme property: {PropertyName}", property.Name);
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