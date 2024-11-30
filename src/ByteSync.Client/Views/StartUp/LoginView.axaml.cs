using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Input;
using ByteSync.ViewModels.StartUp;
using ReactiveUI;
using System.Reactive.Linq;
using ReactiveMarbles.ObservableEvents;

namespace ByteSync.Views.StartUp
{
    partial class LoginView : ReactiveUserControl<LoginViewModel>
    {
        public TextBox Email => this.FindControl<TextBox>("Email");
        
        public TextBox Serial => this.FindControl<TextBox>("Serial");
        
        public Button Connect => this.FindControl<Button>("ButtonSignIn");
        
        public LoginView()
        {
            InitializeComponent();
            
            this.WhenActivated(disposables =>
            {
                // Email.Events().KeyUp
                //     .Select(e => e.Key == Key.Enter)
                //     .InvokeCommand(this, x => x.ViewModel!.SignInCommand)
                //     .DisposeWith(disposables);
                
                Email.Events().KeyUp
                    .Where(k => k.Key == Key.Enter)
                    .Subscribe(e =>
                    {
                        Connect.Focus();
                        ViewModel?.SignInCommand.Execute().Subscribe();
                    })
                    .DisposeWith(disposables);
                
                Serial.Events().KeyUp
                    .Where(k => k.Key == Key.Enter)
                    .Subscribe(e =>
                    {
                        Connect.Focus();
                        ViewModel?.SignInCommand.Execute().Subscribe();
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
