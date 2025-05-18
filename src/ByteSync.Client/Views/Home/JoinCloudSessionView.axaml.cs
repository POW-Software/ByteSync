using Avalonia.Input;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Home;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace ByteSync.Views.Home;

public partial class JoinCloudSessionView : ReactiveUserControl<JoinCloudSessionViewModel>
{
    public JoinCloudSessionView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
        {
            Observable.FromEventPattern<KeyEventArgs>(
                    h => TextBoxSessionId.KeyUp += h,
                    h => TextBoxSessionId.KeyUp -= h)
                .Where(x => x.EventArgs.Key == Key.Enter)
                .Subscribe(e =>
                {
                    ButtonJoin.Focus();
                    ViewModel?.JoinCommand.Execute().Subscribe();
                })
                .DisposeWith(disposables);
                
            Observable.FromEventPattern<KeyEventArgs>(
                    h => TextBoxSessionPassword.KeyUp += h,
                    h => TextBoxSessionPassword.KeyUp -= h)
                .Where(x => x.EventArgs.Key == Key.Enter)
                .Subscribe(e =>
                {
                    ButtonJoin.Focus();
                    ViewModel?.JoinCommand.Execute().Subscribe();
                })
                .DisposeWith(disposables);
        });
    }
}