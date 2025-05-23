﻿using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;

namespace ByteSync.Views.Misc;

public class ActivityIndicator : TemplatedControl
{
    private const string InactiveState = ":inactive";
    private const string ActiveState = ":active";
    
    private double _rectangleWidth = 10;
    private double _rectangleHeight = 10;
    private Thickness _rectangleMargin = new Thickness(1);

    static ActivityIndicator()
    {
        IsActiveProperty.Changed.Subscribe(OnIsActiveChanged);
        IsVisibleProperty.Changed.AddClassHandler<ActivityIndicator>((x, e) => x.OnIsVisibleChanged(e));
    }

    public ActivityIndicator()
    {
        LayoutUpdated += (_, _) =>
        {
            // Update active state only if necessary to avoid infinite loops
            bool shouldBeActive = IsVisible && IsEffectivelyVisible;
            if (IsActive != shouldBeActive)
            {
                IsActive = shouldBeActive;
            }
        };
    }

    #region IsActive

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }
    
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<ActivityIndicator, bool>(nameof(IsActive), defaultValue: false);
    
    private static void OnIsActiveChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Sender is ActivityIndicator activityIndicator)
        {
            activityIndicator.UpdateVisualStates();
        }
    }

    private void OnIsVisibleChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is bool isVisible)
        {
            IsActive = isVisible && IsEffectivelyVisible;
        }
    }
    
    public static readonly DirectProperty<ActivityIndicator, double> RectangleWidthProperty =
        AvaloniaProperty.RegisterDirect<ActivityIndicator, double>(
            nameof(RectangleWidth),
            o => o.RectangleWidth);

    public double RectangleWidth
    {
        get { return _rectangleWidth; }
        private set { SetAndRaise(RectangleWidthProperty, ref _rectangleWidth, value); }
    }
    
    public static readonly DirectProperty<ActivityIndicator, double> RectangleHeightProperty =
        AvaloniaProperty.RegisterDirect<ActivityIndicator, double>(
            nameof(RectangleHeight),
            o => o.RectangleHeight);

    public double RectangleHeight
    {
        get { return _rectangleHeight; }
        private set { SetAndRaise(RectangleHeightProperty, ref _rectangleHeight, value); }
    }

    public static readonly DirectProperty<ActivityIndicator, Thickness> RectangleMarginProperty =
        AvaloniaProperty.RegisterDirect<ActivityIndicator, Thickness>(
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