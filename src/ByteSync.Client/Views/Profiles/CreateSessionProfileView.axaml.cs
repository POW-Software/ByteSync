using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.Common.Helpers;
using ByteSync.ViewModels.Profiles;
using ReactiveUI;

namespace ByteSync.Views.Profiles;

public class CreateSessionProfileView : ReactiveUserControl<CreateSessionProfileViewModel>
{
    public AutoCompleteBox AutoCompleteBoxProfile => this.FindControl<AutoCompleteBox>("AutoCompleteBoxProfile");
    
    public CreateSessionProfileView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {

        });

        // Fonctionne pour mettre le focus, mais un peu lourd. Essayé avec d'autres events (OnAttachedToVisualTree, OnGotFocus), ca ne passait pas
        // Autre piste : https://stackoverflow.com/questions/21211596/focus-on-control-using-reactiveui
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

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}