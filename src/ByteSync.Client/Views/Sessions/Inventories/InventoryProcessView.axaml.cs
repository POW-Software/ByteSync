using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Inventories;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Inventories;

public partial class InventoryProcessView : ReactiveUserControl<InventoryProcessViewModel>
{
    public InventoryProcessView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables => { });
        
    }
}