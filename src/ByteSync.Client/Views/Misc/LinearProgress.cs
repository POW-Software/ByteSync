using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace ByteSync.Views.Misc;

/// <summary>
/// Basé sur https://github.com/Deadpikle/AvaloniaProgressRing/blob/master/AvaloniaProgressRing/Styles/ProgressRing.xaml
/// </summary>
public class LinearProgress : TemplatedControl
{
    private const string InactiveState = ":inactive";
    private const string ActiveState = ":active";
    
    private double _rectangleWidth = 10;
    private double _rectangleHeight = 10;
    private Thickness _rectangleMargin = new Thickness(1);

    static LinearProgress()
    {

    }

    public LinearProgress()
    {
    }

    #region IsActive

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }


    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<LinearProgress, bool>(nameof(IsActive), defaultValue: true, notifying: OnIsActiveChanged);

    private static void OnIsActiveChanged(AvaloniaObject obj, bool arg2)
    {
        ((LinearProgress)obj).UpdateVisualStates();
    }
    
    public static readonly DirectProperty<LinearProgress, double> RectangleWidthProperty =
        AvaloniaProperty.RegisterDirect<LinearProgress, double>(
            nameof(RectangleWidth),
            o => o.RectangleWidth);

    public double RectangleWidth
    {
        get { return _rectangleWidth; }
        private set { SetAndRaise(RectangleWidthProperty, ref _rectangleWidth, value); }
    }
    
    
    public static readonly DirectProperty<LinearProgress, double> RectangleHeightProperty =
        AvaloniaProperty.RegisterDirect<LinearProgress, double>(
            nameof(RectangleHeight),
            o => o.RectangleHeight);

    public double RectangleHeight
    {
        get { return _rectangleHeight; }
        private set { SetAndRaise(RectangleHeightProperty, ref _rectangleHeight, value); }
    }

    public static readonly DirectProperty<LinearProgress, Thickness> RectangleMarginProperty =
        AvaloniaProperty.RegisterDirect<LinearProgress, Thickness>(
            nameof(RectangleMargin),
            o => o.RectangleMargin);

    public Thickness RectangleMargin
    {
        get { return _rectangleMargin; }
        private set { SetAndRaise(RectangleMarginProperty, ref _rectangleMargin, value); }
    }
    
    #endregion


    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        double unitWidth = Width / 11d;
        double rectangleWidth = unitWidth * 2;
        double rightMargin = unitWidth;
        
        // https://stackoverflow.com/questions/24764565/is-there-a-defined-value-in-the-standard-namespaces-for-the-golden-ratio
        const double goldenRatio = 1.61803398874989484820458683436;
        
        // double preferredMaxheight = rectangleWidth * goldenRatio;
        double preferredMaxheight = rectangleWidth;


        RectangleWidth = rectangleWidth;
        if (preferredMaxheight < Height)
        {
            RectangleHeight = preferredMaxheight;
        }
        else
        {
            RectangleHeight = Height;
        }
        RectangleMargin = new Thickness(0, 0, rightMargin, 0);
        
        UpdateVisualStates();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        if (IsActive)
        {
            IsActive = false;
        }
        
        base.OnDetachedFromLogicalTree(e);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        UpdateVisualStates();
    }

    private void UpdateVisualStates()
    {
        PseudoClasses.Remove(ActiveState);
        PseudoClasses.Remove(InactiveState);
        PseudoClasses.Add(IsActive ? ActiveState : InactiveState);
    }
}