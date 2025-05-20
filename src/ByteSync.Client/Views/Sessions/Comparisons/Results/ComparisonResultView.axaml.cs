using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using DynamicData;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Comparisons.Results;

public partial class ComparisonResultView : ReactiveUserControl<ComparisonResultViewModel>
{
    public ComparisonResultView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
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
    
    private void TagEditor_TagsChanged(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.OnTagsChanged();
        }
    }
}