using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace ByteSync.Views.Behaviors;

// trouvé sur https://github.com/AvaloniaUI/Avalonia/issues/8903
public class AutoCompleteZeroMinimumPrefixLengthDropdownBehavior : Behavior<AutoCompleteBox>
    {
        static AutoCompleteZeroMinimumPrefixLengthDropdownBehavior()
        {
        }

        protected override void OnAttached()
        {
            if (AssociatedObject is not null)
            {
                AssociatedObject.KeyUp += OnKeyUp; ;
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

        //have to use KeyUp as AutoCompleteBox eats some of the KeyDown events
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
            if (AssociatedObject is not null && !AssociatedObject.IsDropDownOpen)
            {
                typeof(AutoCompleteBox).GetMethod("PopulateDropDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .Invoke(AssociatedObject, new object[] { AssociatedObject, EventArgs.Empty });
                
                typeof(AutoCompleteBox).GetMethod("OpeningDropDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .Invoke(AssociatedObject, new object[] { false });
                
                //opening the dropdown does not set this automatically.
                //We *must* set the field and not the property as we need to avoid the changed event being raised (which prevents the dropdown opening).
                var iddo = typeof(AutoCompleteBox).GetField("_isDropDownOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if ((bool)iddo?.GetValue(AssociatedObject) == false)
                    iddo?.SetValue(AssociatedObject, true);
            }
        }
    }