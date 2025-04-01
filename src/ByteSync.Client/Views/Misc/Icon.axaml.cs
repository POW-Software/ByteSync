using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace ByteSync.Views.Misc;

// Voir Projektanker.Icons.Avalonia.Icon
public class Icon : TemplatedControl
{
    public static readonly DirectProperty<Icon, DrawingImage> DrawingImageProperty =
        AvaloniaProperty.RegisterDirect<Icon, DrawingImage>(nameof(DrawingImage), o => o.DrawingImage);

    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<Icon, string>(nameof(Value));

    private DrawingImage _drawingImage;

    static Icon()
    {
        ValueProperty.Changed
            .Select(e => e.Sender)
            .OfType<Icon>()
            .Subscribe(icon => icon.OnValueChanged());

        ForegroundProperty.Changed
            .Select(e => e.Sender)
            .OfType<Icon>()
            .Subscribe(icon => icon.OnForegroundChanged());
    }

    public DrawingImage DrawingImage
    {
        get => _drawingImage;
        private set => SetAndRaise(DrawingImageProperty, ref _drawingImage, value);
    }

    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private void OnValueChanged()
    {
        if (Value == null)
        {
            // 23/11/2021 : Survient lorsqu'on change de thème, raison inconnue pour le moment
            return;
        }
            
        string value;
        if (Value.StartsWith("BoxIcons."))
        {
            value = Value;
        }
        else
        {
            value = "BoxIcons." + Value;
        }

        object? styleResource = null;
        Application.Current?.Styles.TryGetResource(value, out styleResource);
        // var resource = App.Current.Tr["BoxIcons.LogosDigitalocean"];
            
        // string path = IconProvider.GetIconPath(Value);
        // var geometry = styleResource as GeometryDrawing;
            
        if (styleResource is GeometryDrawing geometryDrawing)
        {
            var drawing = new GeometryDrawing()
            {
                Geometry = geometryDrawing.Geometry, // Geometry.Parse(path),
                Brush = Foreground ?? new SolidColorBrush(0),
            };

            DrawingImage = new DrawingImage { Drawing = drawing };
        }
    }

    private void OnForegroundChanged()
    {
        if (DrawingImage?.Drawing is GeometryDrawing geometryDrawing)
        {
            DrawingImage.Drawing = new GeometryDrawing
            {
                Geometry = geometryDrawing.Geometry,
                Brush = Foreground,
            };
        }
    }
}