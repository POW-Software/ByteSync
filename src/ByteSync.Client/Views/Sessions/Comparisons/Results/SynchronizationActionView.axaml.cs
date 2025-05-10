using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ByteSync.Views.Sessions.Comparisons.Results;

public partial class SynchronizationActionView : UserControl
{
    public SynchronizationActionView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}