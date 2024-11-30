using Avalonia.Media;
using ByteSync.Business.Themes;

namespace ByteSync.Interfaces.Controls.Themes;

public interface IThemeService
{
    List<Theme> AvailableThemes { get; }
    
    IObservable<Theme> SelectedTheme { get; }
    
    public void OnThemesRegistred();
    
    void RegisterTheme(Theme theme);
    
    void GetResource<T>(string resourceName, out T? resource);

    IBrush? GetBrush(string resourceName);
    
    void SelectTheme(string? name, bool isDarkMode);
}