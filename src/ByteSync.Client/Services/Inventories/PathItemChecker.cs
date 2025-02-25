using ByteSync.Assets.Resources;
using ByteSync.Business.PathItems;
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
    
    public async Task<bool> CheckPathItem(PathItem pathItem, IEnumerable<PathItem> existingPathItems)
    {
        if (pathItem.Type == FileSystemTypes.File)
        {
            if (existingPathItems.Any(pi => pi.ClientInstanceId.Equals(pathItem.ClientInstanceId) && pi.Type == FileSystemTypes.File
                      && pi.Path.Equals(pathItem.Path, StringComparison.InvariantCultureIgnoreCase)))
            {
                await ShowError();

                return false;
            }
        }
        else
        {
            // We can neither be equal, nor be, nor be a parent of an already selected path
            if (existingPathItems.Any(pi => pi.ClientInstanceId.Equals(pathItem.ClientInstanceId) && pi.Type == FileSystemTypes.Directory
                        && (pi.Path.Equals(pathItem.Path, StringComparison.InvariantCultureIgnoreCase) || 
                            IOUtils.IsSubPathOf(pi.Path, pathItem.Path) || 
                            IOUtils.IsSubPathOf(pathItem.Path, pi.Path))))
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