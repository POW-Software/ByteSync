using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia;
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
    
    private readonly BehaviorSubject<Theme> _selectedTheme;

    public ThemeService(IApplicationSettingsRepository applicationSettingsManager)
    {
        _applicationSettingsRepository = applicationSettingsManager;

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
            fluentTheme.Resources["SystemAccentColor"] = theme.ThemeColor.AvaloniaColor;
            
            Application.Current.RequestedThemeVariant = theme.IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
            
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
        object? styleResource = null;
        Application.Current?.Styles.TryGetResource(resourceName, ThemeVariant.Default, out styleResource);

        if (styleResource is T)
        {
            resource = (T) styleResource;
        }
        else
        {
            resource = default;
        }
    }
    
    public IBrush? GetBrush(string resourceName)
    {
        object? styleResource = null;
        Application.Current?.Styles.TryGetResource(resourceName, ThemeVariant.Default,  out styleResource);

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
            return null;
        }
    }
}