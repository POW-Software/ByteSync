using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Members;

namespace ByteSync.Views.Sessions.Members;

public partial class DataNodeView : ReactiveUserControl<DataNodeViewModel>
{
    public DataNodeView()
    {
        InitializeComponent();
    }
}