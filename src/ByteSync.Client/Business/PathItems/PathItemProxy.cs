using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Interfaces;
using ByteSync.Interfaces;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.Business.PathItems;

public class PathItemProxy : ReactiveObject, IDisposable
{
    private readonly ILocalizationService _localizationService;
    private readonly IFileSystemAccessor _fileSystemAccessor;
    private readonly ILogger<PathItemProxy> _logger;
    
    private readonly CompositeDisposable _disposables = new();

    public PathItemProxy()
    {

    }
    
    public PathItemProxy(PathItem pathItem, ILocalizationService localizationService, IFileSystemAccessor fileSystemAccessor,
        ILogger<PathItemProxy> logger)
    {
        _localizationService = localizationService;
        _fileSystemAccessor = fileSystemAccessor;
        _logger = logger;

        PathItem = pathItem;
        Code = PathItem.Code;
        Path = PathItem.Path;
        FileSystemType = PathItem.Type;

        UpdateElementType();

        OpenPathCommand = ReactiveCommand.Create(OpenPath);

        var localizationSubscription = _localizationService.CurrentCultureObservable
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => UpdateElementType());
        _disposables.Add(localizationSubscription);
        
        var codeSubscription = PathItem
            .WhenAnyValue(x => x.Code)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(newCode => Code = newCode);
        _disposables.Add(codeSubscription);
    }
    
    public PathItem PathItem { get; } = null!;
    
    [Reactive]
    public string Code { get; set; } = null!;
    
    [Reactive]
    public string ElementType { get; set; } = null!;
    
    [Reactive]
    public string Path { get; set; }
    
    [Reactive]
    public FileSystemTypes FileSystemType { get; set; }
    
    public ReactiveCommand<Unit, Unit> OpenPathCommand { get; set; }
    
    private void UpdateElementType()
    {
        if (FileSystemType == FileSystemTypes.Directory)
        {
            ElementType = _localizationService[nameof(Resources.General_Directory)];
        }
        else
        {
            ElementType = _localizationService[nameof(Resources.General_File)];
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

    public void Dispose()
    {
        _disposables.Dispose();
    }
}