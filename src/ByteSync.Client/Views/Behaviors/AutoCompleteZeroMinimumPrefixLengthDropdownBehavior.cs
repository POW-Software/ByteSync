using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace ByteSync.Views.Behaviors;

public class AutoCompleteZeroMinimumPrefixLengthDropdownBehavior : Behavior<AutoCompleteBox>
{
    static AutoCompleteZeroMinimumPrefixLengthDropdownBehavior()
    {
    }

    protected override void OnAttached()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.KeyUp += OnKeyUp;
            AssociatedObject.PointerReleased += OnPointerReleased;
        }

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.KeyUp -= OnKeyUp;
            AssociatedObject.PointerReleased -= OnPointerReleased;
        }

        base.OnDetaching();
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Down or Key.F4 or Key.Enter 
            && string.IsNullOrEmpty(AssociatedObject?.Text) 
            && (!AssociatedObject?.IsDropDownOpen ?? false))
        {
            ShowDropdown();
        }
    }
    
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left
            && string.IsNullOrEmpty(AssociatedObject?.Text)
            && (!AssociatedObject?.IsDropDownOpen ?? false))
        {
            ShowDropdown();
        }
    }

    private void ShowDropdown()
    {
        // Cette méthode devra être adaptée pour Avalonia 11
        // car le fonctionnement interne de AutoCompleteBox a changé
        if (AssociatedObject is not null && !AssociatedObject.IsDropDownOpen)
        {
            // La façon d'accéder aux méthodes privées a changé dans Avalonia 11
            // Il faudra adapter cette partie selon la nouvelle implémentation
            AssociatedObject.IsDropDownOpen = true;
        }
    }
}