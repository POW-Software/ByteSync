using System.Threading.Tasks;
using ByteSync.Assets.Resources;
using ByteSync.Business.PathItems;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Helpers;
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
            return true;
        }

        // We can neither be equal, nor be, nor be a parent of an already selected path
        if (existingPathItems.Any(pi => pi.Path.Equals(pathItem.Path, StringComparison.InvariantCultureIgnoreCase) || 
                                IOUtils.IsSubPathOf(pi.Path, pathItem.Path) || 
                                IOUtils.IsSubPathOf(pathItem.Path, pi.Path)))
        {
            var messageBoxViewModel = _dialogService.CreateMessageBoxViewModel(
                nameof(Resources.PathItemChecker_SubPathError_Title), nameof(Resources.PathItemChecker_SubPathError_Message));
            messageBoxViewModel.ShowOK = true;
            await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);
            
            return false;
        }

        return true;
    }
}