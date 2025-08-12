using ByteSync.Assets.Resources;
using ByteSync.Business.DataSources;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Dialogs;

namespace ByteSync.Services.Inventories;

public class DataSourceChecker : IDataSourceChecker
{
    private readonly IDialogService _dialogService;
    
    public DataSourceChecker(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }
    
    public async Task<bool> CheckDataSource(DataSource dataSource, IEnumerable<DataSource> existingDataSources)
    {
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
}