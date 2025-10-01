using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Inventories;

namespace ByteSync.Views.Sessions.Inventories;

public partial class InventoryGlobalStatusView : ReactiveUserControl<InventoryGlobalStatusViewModel>
{
    public InventoryGlobalStatusView()
    {
        InitializeComponent();
    }
}