using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Home;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace ByteSync.Views.Home;

public partial class JoinCloudSessionView : ReactiveUserControl<JoinCloudSessionViewModel>
{
    // public TextBox TextBoxSessionId => this.FindControl<TextBox>("TextBoxSessionId");
    //     
    // public TextBox TextBoxSessionPassword => this.FindControl<TextBox>("TextBoxSessionPassword");
    //     
    // public Button ButtonJoin => this.FindControl<Button>("ButtonJoin");
    
    // private TextBox TextBoxSessionId;
    
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
    
    // private void InitializeComponent()
    // {
    //     AvaloniaXamlLoader.Load(this);
    // }
}