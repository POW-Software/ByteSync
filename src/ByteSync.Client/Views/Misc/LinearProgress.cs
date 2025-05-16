using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;

namespace ByteSync.Views.Misc;

public class LinearProgress : TemplatedControl
{
    private const string InactiveState = ":inactive";
    private const string ActiveState = ":active";
    
    private double _rectangleWidth = 10;
    private double _rectangleHeight = 10;
    private Thickness _rectangleMargin = new Thickness(1);

    static LinearProgress()
    {
        IsActiveProperty.Changed.Subscribe(OnIsActiveChanged);
        IsVisibleProperty.Changed.AddClassHandler<LinearProgress>((x, e) => x.OnIsVisibleChanged(e));
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
        AvaloniaProperty.Register<LinearProgress, bool>(nameof(IsActive), defaultValue: false);
    
    private static void OnIsActiveChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Sender is LinearProgress linearProgress)
        {
            linearProgress.UpdateVisualStates();
        }
    }

    private void OnIsVisibleChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is bool isVisible)
        {
            IsActive = isVisible && IsEffectivelyVisible;
        }
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

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        IsActive = IsVisible;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        IsActive = false;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        double unitWidth = Width / 11d;
        double rectangleWidth = unitWidth * 2;
        double rightMargin = unitWidth;
        
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

    private void UpdateVisualStates()
    {
        PseudoClasses.Remove(ActiveState);
        PseudoClasses.Remove(InactiveState);
        PseudoClasses.Add(IsActive ? ActiveState : InactiveState);
    }
}