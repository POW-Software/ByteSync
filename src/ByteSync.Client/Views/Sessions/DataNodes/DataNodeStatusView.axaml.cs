using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.DataNodes;

namespace ByteSync.Views.Sessions.DataNodes;

public partial class DataNodeStatusView : ReactiveUserControl<DataNodeStatusViewModel>
{
    public DataNodeStatusView()
    {
        InitializeComponent();
    }
} 