using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Inventories;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Inventories;

public partial class InventoryMainView : ReactiveUserControl<InventoryMainViewModel>
{
    public InventoryMainView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables => { });
    }
}