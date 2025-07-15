using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Members.Status;

public class DataNodeStatusViewModel : ReactiveObject
{
    [Reactive]
    public string Status { get; set; } = string.Empty;

    [Reactive]
    public bool IsLocalMachine { get; set; }
} 