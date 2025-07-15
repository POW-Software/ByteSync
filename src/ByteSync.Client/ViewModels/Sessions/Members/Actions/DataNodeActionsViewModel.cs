using System;
using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Members.Actions;

public class DataNodeActionsViewModel : ReactiveObject
{
    public ReactiveCommand<Unit, Unit> AddDirectoryCommand { get; set; }
    public ReactiveCommand<Unit, Unit> AddFileCommand { get; set; }

    [Reactive]
    public bool IsFileSystemSelectionEnabled { get; set; }

    [Reactive]
    public bool IsLocalMachine { get; set; }

    [Reactive]
    public int DataSourcesCount { get; set; }

    public DataNodeActionsViewModel() { }

    public DataNodeActionsViewModel(ReactiveCommand<Unit, Unit> addDirectoryCommand, ReactiveCommand<Unit, Unit> addFileCommand, bool isFileSystemSelectionEnabled, bool isLocalMachine, int dataSourcesCount)
    {
        AddDirectoryCommand = addDirectoryCommand;
        AddFileCommand = addFileCommand;
        IsFileSystemSelectionEnabled = isFileSystemSelectionEnabled;
        IsLocalMachine = isLocalMachine;
        DataSourcesCount = dataSourcesCount;
    }
} 