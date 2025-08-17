using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Interfaces;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Services.Localizations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.Business.DataSources;

public class DataSourceProxy : ReactiveObject, IDisposable
{
    private readonly ILocalizationService _localizationService;
    private readonly IFileSystemAccessor _fileSystemAccessor;
    private readonly ILogger<DataSourceProxy> _logger;
    
    private readonly CompositeDisposable _disposables = new();

    public DataSourceProxy()
    {

    }
    
    public DataSourceProxy(DataSource dataSource, ILocalizationService localizationService, IFileSystemAccessor fileSystemAccessor,
        ILogger<DataSourceProxy> logger)
    {
        _localizationService = localizationService;
        _fileSystemAccessor = fileSystemAccessor;
        _logger = logger;

        DataSource = dataSource;
        Code = DataSource.Code;
        Path = DataSource.Path;
        FileSystemType = DataSource.Type;

        UpdateElementType();

        OpenPathCommand = ReactiveCommand.Create(OpenPath);

        var localizationSubscription = _localizationService.CurrentCultureObservable
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => UpdateElementType());
        _disposables.Add(localizationSubscription);
        
        var codeSubscription = DataSource
            .WhenAnyValue(x => x.Code)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(newCode => Code = newCode);
        _disposables.Add(codeSubscription);
    }
    
    public DataSource DataSource { get; } = null!;
    
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
                _fileSystemAccessor.OpenDirectory(DataSource.Path);
            }
            else
            {
                _fileSystemAccessor.OpenDirectoryAndSelectFile(DataSource.Path);
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