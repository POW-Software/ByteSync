using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ByteSync.Views.Sessions.Comparisons.Results
{
    public class SynchronizationRuleSummaryView : UserControl
    {
        public SynchronizationRuleSummaryView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}