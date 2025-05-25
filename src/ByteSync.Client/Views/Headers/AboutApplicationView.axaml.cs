using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Headers;

namespace ByteSync.Views.Headers;

public partial class AboutApplicationView : ReactiveUserControl<AboutApplicationViewModel>
{
    public AboutApplicationView()
    {
        InitializeComponent();
    }
}