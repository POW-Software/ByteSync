using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.DataNodes;

namespace ByteSync.Views.Sessions.DataNodes;

public partial class DataNodeHeaderView : ReactiveUserControl<DataNodeHeaderViewModel>
{
    public DataNodeHeaderView()
    {
        InitializeComponent();
    }
} 