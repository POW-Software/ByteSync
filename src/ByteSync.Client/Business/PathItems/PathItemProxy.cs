using System.Reactive;
using ByteSync.Assets.Resources;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Interfaces;
using ByteSync.Interfaces;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.Business.PathItems;

public class PathItemProxy
{
    private readonly IFileSystemAccessor _fileSystemAccessor;
    private readonly ILogger<PathItemProxy> _logger;

    public PathItemProxy()
    {

    }
    
    public PathItemProxy(PathItem pathItem, ILocalizationService localizationService, IFileSystemAccessor fileSystemAccessor,
        ILogger<PathItemProxy> logger)
    {
        _fileSystemAccessor = fileSystemAccessor;
        _logger = logger;

        PathItem = pathItem;
        // Code = PathItem.Code;
        Path = PathItem.Path;
        FileSystemType = PathItem.Type;

        UpdateElementType(localizationService);

        OpenPathCommand = ReactiveCommand.Create(OpenPath);

        // this.WhenAnyValue(x => x.Code)
        //     .Skip(1)
        //     .Subscribe(code => PathItem.Code = code);
    }
    
    public PathItem PathItem { get; } = null!;
    
    public string ElementType { get; set; } = null!;

    // [Reactive]
    // public string Code { get; set; }
    
    public string Path { get; set; }
    
    [Reactive]
    public FileSystemTypes FileSystemType { get; set; }
    
    public ReactiveCommand<Unit, Unit> OpenPathCommand { get; set; }

    public void OnLocaleChanged(ILocalizationService localizationService)
    {
        UpdateElementType(localizationService);
    }
    
    private void UpdateElementType(ILocalizationService localizationService)
    {
        if (FileSystemType == FileSystemTypes.Directory)
        {
            ElementType = localizationService[nameof(Resources.General_Directory)];
        }
        else
        {
            ElementType = localizationService[nameof(Resources.General_File)];
        }
    }

    private void OpenPath()
    {
        try
        {
            if (FileSystemType == FileSystemTypes.Directory)
            {
                _fileSystemAccessor.OpenDirectory(PathItem.Path);
            }
            else
            {
                _fileSystemAccessor.OpenDirectoryAndSelectFile(PathItem.Path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "error during OpenPath");
        }

    }

    // public static void RecodePathItems(ObservableCollection<PathItemViewModel> pathItemViewModels, string letter)
    // {
    //     int cpt = 1;
    //     foreach (var pathItem in pathItemViewModels)
    //     {
    //         pathItem.Code = letter + cpt;
    //
    //         cpt += 1;
    //     }
    // }
}