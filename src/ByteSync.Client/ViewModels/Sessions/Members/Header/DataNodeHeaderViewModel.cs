using System;
using Avalonia.Media;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Members.Header;

public class DataNodeHeaderViewModel : ReactiveObject
{
    [Reactive]
    public int PositionInList { get; set; }

    [Reactive]
    public string MachineDescription { get; set; } = string.Empty;

    [Reactive]
    public string ClientInstanceId { get; set; } = string.Empty;

    [Reactive]
    public IBrush LetterBackBrush { get; set; }

    [Reactive]
    public IBrush LetterBorderBrush { get; set; }

    [Reactive]
    public bool IsLocalMachine { get; set; }

    public DataNodeHeaderViewModel() { }

    public DataNodeHeaderViewModel(int positionInList, string machineDescription, string clientInstanceId, IBrush letterBackBrush, IBrush letterBorderBrush)
    {
        PositionInList = positionInList;
        MachineDescription = machineDescription;
        ClientInstanceId = clientInstanceId;
        LetterBackBrush = letterBackBrush;
        LetterBorderBrush = letterBorderBrush;
    }
} 