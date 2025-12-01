using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Ratings;

namespace ByteSync.Views.Ratings;

public partial class RatingPromptView : ReactiveUserControl<RatingPromptViewModel>
{
    public RatingPromptView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
