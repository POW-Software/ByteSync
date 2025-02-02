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

public class JoinCloudSessionView : ReactiveUserControl<JoinCloudSessionViewModel>
{
    public TextBox TextBoxSessionId => this.FindControl<TextBox>("TextBoxSessionId");
        
    public TextBox TextBoxSessionPassword => this.FindControl<TextBox>("TextBoxSessionPassword");
        
    public Button ButtonJoin => this.FindControl<Button>("ButtonJoin");
    
    public JoinCloudSessionView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
        {
            TextBoxSessionId.Events().KeyUp
                .Where(k => k.Key == Key.Enter)
                .Subscribe(e =>
                {
                    ButtonJoin.Focus();
                    ViewModel?.JoinCommand.Execute().Subscribe();
                })
                .DisposeWith(disposables);
                
            TextBoxSessionPassword.Events().KeyUp
                .Where(k => k.Key == Key.Enter)
                .Subscribe(e =>
                {
                    ButtonJoin.Focus();
                    ViewModel?.JoinCommand.Execute().Subscribe();
                })
                .DisposeWith(disposables);
        });
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}