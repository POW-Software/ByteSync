using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ByteSync.Views.Sessions.Comparisons.Results
{
    public class StatusView : UserControl
    {
        public StatusView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}