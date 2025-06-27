using System.Threading.Tasks;

namespace ByteSync.Common.Interfaces;

public interface IFileSystemAccessor
{
    Task OpenFile(string path);
        
    Task OpenDirectory(string path);
        
    Task OpenDirectoryAndSelectFile(string dataSourcePath);
}