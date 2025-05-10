using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using DynamicData;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Comparisons.Results;

public partial class ComparisonResultView : ReactiveUserControl<ComparisonResultViewModel>
{
    public DataGrid DataGrid => this.FindControl<DataGrid>("DataGrid");
        
    public ComparisonResultView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void TheGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var l = DataGrid.SelectedItems;

        List<ComparisonItemViewModel> toRemove = new List<ComparisonItemViewModel>();
        foreach (var item in ViewModel!.SelectedItems)
        {
            if (!l.Contains(item))
            {
                toRemove.Add(item);
            }
        }
        ViewModel.SelectedItems.Remove(toRemove);
            
        foreach (var item in l)
        {
            if (item is ComparisonItemViewModel comparisonItemViewModel)
            {
                if (!ViewModel.SelectedItems.Contains(comparisonItemViewModel))
                {
                    ViewModel.SelectedItems.Add(comparisonItemViewModel);
                }
            }
        }
    }
}