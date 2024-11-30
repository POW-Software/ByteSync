using Avalonia.Styling;

namespace ByteSync.Business.Themes
{
    public class Theme
    {
        public Theme(string themeName, ThemeModes mode, Styles style)
        {
            Name = themeName;
            Mode = mode;
            Style = style;
        }
        
        public string Name { get; }
        
        public ThemeModes Mode { get; }
        
        public Styles Style { get; }

        public string Key
        {
            get
            {
                return $"{Mode}.{Name}";
            }
        }

        public bool IsDarkMode
        {
            get
            {
                return Mode == ThemeModes.Dark;
            }
        }
    }
}