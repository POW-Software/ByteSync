using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ByteSync.Views.Lobbies;

public partial class LobbySynchronizationRuleView : UserControl
{
    public LobbySynchronizationRuleView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}