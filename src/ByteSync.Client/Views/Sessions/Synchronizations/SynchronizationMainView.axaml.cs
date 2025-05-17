using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls.Mixins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Synchronizations;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Synchronizations;

public partial class SynchronizationMainView : ReactiveUserControl<SynchronizationMainViewModel>
{
    public SynchronizationMainView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
                

            
        });
    }
}