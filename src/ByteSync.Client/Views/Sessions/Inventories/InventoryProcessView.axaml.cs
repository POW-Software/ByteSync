using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Inventories;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Inventories;

public partial class InventoryProcessView : ReactiveUserControl<InventoryProcessViewModel>
{
    public InventoryProcessView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();

    #if DEBUG
        this.WhenAnyValue(x => x.Bounds)
            .Subscribe(bounds => BoundsChanged(bounds));
    #endif
    }

#if DEBUG
    private void BoundsChanged(Rect bounds)
    {
        // 05/04/2022: Permet de récupérer facilement la hauteur si jamais ce panneau venait à changer de taille
    }
#endif

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}