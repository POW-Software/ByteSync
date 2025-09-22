using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Inventories;

namespace ByteSync.Views.Sessions.Inventories;

public partial class InventoryLocalAnalysisView : ReactiveUserControl<InventoryLocalAnalysisViewModel>
{
    public InventoryLocalAnalysisView()
    {
        InitializeComponent();
    }
}