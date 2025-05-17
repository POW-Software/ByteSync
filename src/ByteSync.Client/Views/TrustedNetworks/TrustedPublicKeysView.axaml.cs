using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.TrustedNetworks;

namespace ByteSync.Views.TrustedNetworks;

public partial class TrustedPublicKeysView : ReactiveUserControl<TrustedPublicKeysViewModel>
{
    public TrustedPublicKeysView()
    {
        InitializeComponent();
    }

    // 12/01/2022: For now, we're handling sorting this way. It's not great, but it works in DEBUG mode.
    // Only works if there are items in the list.
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