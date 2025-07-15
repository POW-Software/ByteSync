using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ByteSync.Business.DataSources;

namespace ByteSync.ViewModels.Sessions.Members.Sources;

public class DataNodeSourcesViewModel : ReactiveObject
{
    public ObservableCollection<DataSourceProxy> DataSources { get; set; } = new();

    public ReactiveCommand<DataSourceProxy, Unit> RemoveDataSourceCommand { get; set; }

    [Reactive]
    public bool IsLocalMachine { get; set; }

    [Reactive]
    public bool IsFileSystemSelectionEnabled { get; set; }

    public DataNodeSourcesViewModel() { }

    public DataNodeSourcesViewModel(ObservableCollection<DataSourceProxy> dataSources, ReactiveCommand<DataSourceProxy, Unit> removeDataSourceCommand, bool isLocalMachine, bool isFileSystemSelectionEnabled)
    {
        DataSources = dataSources;
        RemoveDataSourceCommand = removeDataSourceCommand;
        IsLocalMachine = isLocalMachine;
        IsFileSystemSelectionEnabled = isFileSystemSelectionEnabled;
    }
} 