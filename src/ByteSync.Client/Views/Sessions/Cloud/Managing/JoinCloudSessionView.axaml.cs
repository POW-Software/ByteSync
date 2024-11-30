using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Cloud.Managing;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Cloud.Managing
{
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
}