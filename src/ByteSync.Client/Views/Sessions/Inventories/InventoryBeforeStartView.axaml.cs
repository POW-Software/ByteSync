using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Inventories;

namespace ByteSync.Views.Sessions.Inventories;

public partial class InventoryBeforeStartView : ReactiveUserControl<InventoryBeforeStartViewModel>
{
    public InventoryBeforeStartView()
    {
        InitializeComponent();
    }
}