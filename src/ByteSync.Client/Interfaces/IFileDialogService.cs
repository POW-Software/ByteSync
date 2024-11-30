using System.Threading.Tasks;

namespace ByteSync.Interfaces
{
    public interface IFileDialogService
    {
        Task<string[]?> ShowOpenFileDialogAsync(string title, bool allowMultiple);
        
        Task<string?> ShowOpenFolderDialogAsync(string title);
    }
}