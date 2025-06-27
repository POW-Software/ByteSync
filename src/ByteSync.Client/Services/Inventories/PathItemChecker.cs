using ByteSync.Assets.Resources;
using ByteSync.Business.DataSources;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Dialogs;

namespace ByteSync.Services.Inventories;

public class PathItemChecker : IPathItemChecker
{
    private readonly IDialogService _dialogService;
    
    public PathItemChecker(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }
    
    public async Task<bool> CheckPathItem(DataSource dataSource, IEnumerable<DataSource> existingDataSources)
    {
        if (dataSource.Type == FileSystemTypes.File)
        {
            if (existingDataSources.Any(pi => pi.ClientInstanceId.Equals(dataSource.ClientInstanceId) && pi.Type == FileSystemTypes.File
                      && pi.Path.Equals(dataSource.Path, StringComparison.InvariantCultureIgnoreCase)))
            {
                await ShowError();

                return false;
            }
        }
        else
        {
            // We can neither be equal, nor be, nor be a parent of an already selected path
            if (existingDataSources.Any(pi => pi.ClientInstanceId.Equals(dataSource.ClientInstanceId) && pi.Type == FileSystemTypes.Directory
                        && (pi.Path.Equals(dataSource.Path, StringComparison.InvariantCultureIgnoreCase) || 
                            IOUtils.IsSubPathOf(pi.Path, dataSource.Path) || 
                            IOUtils.IsSubPathOf(dataSource.Path, pi.Path))))
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
            nameof(Resources.PathItemChecker_SubPathError_Title), nameof(Resources.PathItemChecker_SubPathError_Message));
        messageBoxViewModel.ShowOK = true;
        await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);
    }
}