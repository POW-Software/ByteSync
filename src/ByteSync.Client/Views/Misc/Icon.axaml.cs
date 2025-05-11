using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace ByteSync.Views.Misc;

public partial class Icon : UserControl
{
    private Image _iconImage;

    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<Icon, string>(nameof(Value));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<Icon, double>(nameof(FontSize), 16);

    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public Icon()
    {
        InitializeComponent();
        _iconImage = this.FindControl<Image>("IconImage");

        // S'abonner aux changements de propriété
        this.GetObservable(ValueProperty).Subscribe(_ => UpdateIcon());
        this.GetObservable(ForegroundProperty).Subscribe(_ => UpdateIcon());
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (string.IsNullOrEmpty(Value))
        {
            _iconImage.Source = null;
            return;
        }

        string key;
        if (Value.StartsWith("BoxIcons."))
        {
            key = Value;
        }
        else
        {
            key = "BoxIcons." + Value;
        }

        object? styleResource = null;
        if (Application.Current?.Styles.TryGetResource(key, ThemeVariant.Default,  out styleResource) == true)
        {
            if (styleResource is GeometryDrawing geometryDrawing)
            {
                var drawing = new GeometryDrawing()
                {
                    Geometry = geometryDrawing.Geometry,
                    Brush = Foreground ?? new SolidColorBrush(Colors.Black),
                };

                _iconImage.Source = new DrawingImage { Drawing = drawing };
            }
            else
            {
                Console.WriteLine($"Resource found but not a GeometryDrawing: {key}");
            }
        }
        else
        {
            Console.WriteLine($"Icon resource not found: {key}");
        }
    }
}