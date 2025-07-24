using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.DataNodes;

namespace ByteSync.Views.Sessions.DataNodes;

public partial class DataNodeView : ReactiveUserControl<DataNodeViewModel>
{
    public DataNodeView()
    {
        InitializeComponent();
    }
}