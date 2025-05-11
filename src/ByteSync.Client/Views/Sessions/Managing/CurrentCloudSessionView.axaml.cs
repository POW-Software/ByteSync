using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Managing;
using ByteSync.Views.Misc;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Managing;

public partial class CurrentCloudSessionView : ReactiveUserControl<CurrentCloudSessionViewModel>
{
    public CurrentCloudSessionView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
        
    private void MainScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        sender.AutoSetPadding();
    }
}