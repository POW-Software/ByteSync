using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Inventories;

namespace ByteSync.Views.Sessions.Inventories;

public partial class InventoryLocalStatusView : ReactiveUserControl<InventoryLocalStatusViewModel>
{
    public InventoryLocalStatusView()
    {
        InitializeComponent();
    }
}