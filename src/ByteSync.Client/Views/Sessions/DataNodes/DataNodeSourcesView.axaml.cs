using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.DataNodes;

namespace ByteSync.Views.Sessions.DataNodes;

public partial class DataNodeSourcesView : ReactiveUserControl<DataNodeSourcesViewModel>
{
    public DataNodeSourcesView()
    {
        InitializeComponent();
    }
} 