using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Home;

namespace ByteSync.Views.Home;

public partial class CreateCloudSessionView : ReactiveUserControl<CreateCloudSessionViewModel>
{
    public CreateCloudSessionView()
    {
        InitializeComponent();
    }
}