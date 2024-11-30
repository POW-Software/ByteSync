using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Cloud.Members;

namespace ByteSync.Views.Sessions.Cloud.Members
{
    class SessionMachineView : ReactiveUserControl<SessionMachineViewModel>
    {
        public SessionMachineView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}