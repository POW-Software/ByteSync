using ByteSync.Assets.Resources;
using ByteSync.Business.DataSources;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Helpers;
using ByteSync.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Dialogs;
using Microsoft.Extensions.Logging;

namespace ByteSync.Services.Inventories;

public class DataSourceChecker : IDataSourceChecker
{
    private readonly IDialogService _dialogService;
    private readonly IEnvironmentService _environmentService;
    private readonly ILogger<DataSourceChecker> _logger;
    
    public DataSourceChecker(IDialogService dialogService, IEnvironmentService environmentService, ILogger<DataSourceChecker> logger)
    {
        _dialogService = dialogService;
        _environmentService = environmentService;
        _logger = logger;
    }
    
    public async Task<bool> CheckDataSource(DataSource dataSource, IEnumerable<DataSource> existingDataSources)
    {
        if (dataSource.ClientInstanceId == _environmentService.ClientInstanceId
            && ProtectedPaths.TryGetProtectedRoot(dataSource.Path, _environmentService.OSPlatform, out var protectedRoot))
        {
            _logger.LogWarning("Blocked data source path {Path} because it is under protected root {ProtectedRoot}",
                dataSource.Path, protectedRoot);
            await ShowProtectedPathError(dataSource.Path);
            
            return false;
        }
        
        if (dataSource.Type == FileSystemTypes.File)
        {
            if (existingDataSources.Any(ds => ds.ClientInstanceId.Equals(dataSource.ClientInstanceId) && ds.Type == FileSystemTypes.File
                      && ds.Path.Equals(dataSource.Path, StringComparison.InvariantCultureIgnoreCase)))
            {
                await ShowError();

                return false;
            }
        }
        else
        {
            // We can neither be equal, nor be, nor be a parent of an already selected path
            if (existingDataSources.Any(ds => ds.ClientInstanceId.Equals(dataSource.ClientInstanceId) && ds.Type == FileSystemTypes.Directory
                        && (ds.Path.Equals(dataSource.Path, StringComparison.InvariantCultureIgnoreCase) || 
                            IOUtils.IsSubPathOf(ds.Path, dataSource.Path) || 
                            IOUtils.IsSubPathOf(dataSource.Path, ds.Path))))
            {
                await ShowError();

                return false;
            }
        }

        return true;
    }

    private async Task ShowError()
    {
        var messageBoxViewModel = _dialogService.CreateMessageBoxViewModel(
            nameof(Resources.DataSourceChecker_SubPathError_Title), nameof(Resources.DataSourceChecker_SubPathError_Message));
        messageBoxViewModel.ShowOK = true;
        await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);
    }
    
    private async Task ShowProtectedPathError(string path)
    {
        var messageBoxViewModel = _dialogService.CreateMessageBoxViewModel(
            nameof(Resources.DataSourceChecker_ProtectedPathError_Title),
            nameof(Resources.DataSourceChecker_ProtectedPathError_Message),
            path);
        messageBoxViewModel.ShowOK = true;
        await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);
    }
}
