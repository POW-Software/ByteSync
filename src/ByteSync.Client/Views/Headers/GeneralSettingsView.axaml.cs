using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Headers;
using ReactiveUI;

namespace ByteSync.Views.Headers;

public partial class GeneralSettingsView : ReactiveUserControl<GeneralSettingsViewModel>
{
    public GeneralSettingsView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
        {
            this
                .OneWayBind(ViewModel, 
                    vm => vm.ZoomLevel, 
                    v => v.tblZoomLevel.Text,
                    value => $"{value} %")
                .DisposeWith(disposables);
        });
    }
}