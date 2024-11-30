using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.TrustedNetworks;

namespace ByteSync.Views.TrustedNetworks;

public class TrustedPublicKeysView : ReactiveUserControl<TrustedPublicKeysViewModel>
{
    public TrustedPublicKeysView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // 01/12/2022 : Pour l'instant, on gère le tri comme ça. C'est pas top, mais ça fonctionne en DEBUG
    // Ne fonctionne que s'il y a des éléments dans la liste
    private void TrustedPublicKeysGrid_OnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (IsInitialSortDone)
        {
            return;
        }
        
        if (sender is DataGrid dataGrid)
        {
            var dateColumn = dataGrid.Columns[2];
            
            dateColumn.Sort(ListSortDirection.Descending);

            IsInitialSortDone = true;
        }
    }

    public bool IsInitialSortDone { get; set; }
}