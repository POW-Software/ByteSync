using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Headers;
using ReactiveUI;

namespace ByteSync.Views.Headers;

public class GeneralSettingsView : ReactiveUserControl<GeneralSettingsViewModel>
{
    public GeneralSettingsView()
    {
        this.WhenActivated(disposables =>
        {
            this
                .OneWayBind(ViewModel, 
                    vm => vm.ZoomLevel, 
                    v => v.tblZoomLevel.Text,
                    value => $"{value} %")
                .DisposeWith(disposables);
        });
            
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
        
    private TextBlock tblZoomLevel => this.FindControl<TextBlock>("tblZoomLevel");
    
    private ToggleSwitch tsDarkMode => this.FindControl<ToggleSwitch>("tsDarkMode");
}