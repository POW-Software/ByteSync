using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Members;

namespace ByteSync.Views.Sessions.Members
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