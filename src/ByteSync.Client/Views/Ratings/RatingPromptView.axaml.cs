using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ByteSync.Views.Ratings;

public partial class RatingPromptView : UserControl
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
