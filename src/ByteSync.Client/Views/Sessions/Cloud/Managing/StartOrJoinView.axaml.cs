using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Cloud.Managing;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Cloud.Managing
{
    public class StartOrJoinView  : ReactiveUserControl<StartOrJoinViewModel>
    {
        public StartOrJoinView()
        {
            this.WhenActivated(disposables => { });
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}