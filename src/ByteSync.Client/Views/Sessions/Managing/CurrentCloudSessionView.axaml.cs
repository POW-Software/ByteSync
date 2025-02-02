using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Managing;
using ByteSync.Views.Misc;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Managing
{
    public class CurrentCloudSessionView : ReactiveUserControl<CurrentCloudSessionViewModel>
    {
        public CurrentCloudSessionView()
        {
            this.WhenActivated(disposables => { });
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void MainScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            sender.AutoSetPadding();
        }
    }
}