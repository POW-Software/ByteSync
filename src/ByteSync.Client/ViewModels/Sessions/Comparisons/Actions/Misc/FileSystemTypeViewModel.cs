using ByteSync.Common.Business.Inventories;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;

public class FileSystemTypeViewModel : ViewModelBase
{
    public FileSystemTypeViewModel()
    {

    }

    public FileSystemTypes FileSystemType { get; set; }

    [Reactive]
    public string Description { get; set; }

    public bool IsFile
    {
        get { return FileSystemType == FileSystemTypes.File; }
    }

    public bool IsDirectory
    {
        get { return FileSystemType == FileSystemTypes.Directory; }
    }
}