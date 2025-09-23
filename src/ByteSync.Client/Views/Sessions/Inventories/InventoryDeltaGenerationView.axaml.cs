using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Inventories;

namespace ByteSync.Views.Sessions.Inventories;

public partial class InventoryDeltaGenerationView : ReactiveUserControl<InventoryDeltaGenerationViewModel>
{
    public InventoryDeltaGenerationView()
    {
        InitializeComponent();
    }
}