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
            // Pas de contrôle particulier sur les fichiers
            return true;
        }

        // var pathItems = _pathItemsService.CurrentMemberPathItems.Items.ToList();

        // if (pathItems == null)
        // {
        //     Log.Information("CheckPathItem: PathItems is null");
        //     
        //     MessageBoxViewModel messageBoxViewModel = new MessageBoxViewModel(
        //         nameof(Resources.General_MessageBox_UnknownError_Title), nameof(Resources.General_MessageBox_UnknownError_Message));
        //     messageBoxViewModel.ShowOK = true;
        //     await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);
        //     
        //     return false;
        // }

        // On ne peut ni être égal, ni un être, ni être un parent d'un chemin déjà sélectionné
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