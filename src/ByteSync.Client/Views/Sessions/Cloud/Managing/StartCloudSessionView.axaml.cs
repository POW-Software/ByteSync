using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Cloud.Managing;

namespace ByteSync.Views.Sessions.Cloud.Managing
{
    public class StartCloudSessionView : ReactiveUserControl<StartCloudSessionViewModel>
    {
        public StartCloudSessionView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}