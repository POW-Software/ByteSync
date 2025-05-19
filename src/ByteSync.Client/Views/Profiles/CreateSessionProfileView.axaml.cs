using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.Common.Helpers;
using ByteSync.ViewModels.Profiles;
using ReactiveUI;

namespace ByteSync.Views.Profiles;

public partial class CreateSessionProfileView : ReactiveUserControl<CreateSessionProfileViewModel>
{
    public CreateSessionProfileView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {

        });

        // Works to set focus, but a little heavy. Tried with other events (OnAttachedToVisualTree, OnGotFocus), but it didn't work.
        // Another possibility : https://stackoverflow.com/questions/21211596/focus-on-control-using-reactiveui
        this.LayoutUpdated += (sender, args) =>
        {
            if (!HasFocused)
            {
                AutoCompleteBoxProfile.Focus();
                HasFocused = true;
            }
        };

        AutoCompleteBoxProfile.TextFilter = (search, item) =>
        {
            if (search.IsEmpty(true))
            {
                return true;
            }
            else
            {
                return item.Contains(search, StringComparison.InvariantCultureIgnoreCase);
            }
        };
    }

    private bool HasFocused { get; set; }
}