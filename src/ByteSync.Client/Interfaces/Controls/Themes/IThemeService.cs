using Avalonia.Media;
using ByteSync.Business.Themes;

namespace ByteSync.Interfaces.Controls.Themes;

public interface IThemeService
{
    List<Theme> AvailableThemes { get; }
    
    IObservable<Theme> SelectedTheme { get; }
    
    public void OnThemesRegistered();
    
    void RegisterTheme(Theme theme);

    IBrush? GetBrush(string resourceName);
    
    void SelectTheme(string? name, bool isDarkMode);
}