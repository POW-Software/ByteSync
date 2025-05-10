using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ByteSync.Views.Sessions.Comparisons.Results;

public partial class ContentIdentityView : UserControl
{
    public ContentIdentityView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}