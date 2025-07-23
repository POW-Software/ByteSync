using System.IO;
using System.Runtime.InteropServices;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;

namespace ByteSync.Services.Comparisons;

public static class IdentityBuilder
{
    public static DirectoryDescription BuildDirectoryDescription(InventoryPart inventoryPart, DirectoryInfo directoryInfo)
    {
        var relativePath = ExtractRelativePath(directoryInfo.FullName, inventoryPart.RootPath); 

        var directoryDescription = new DirectoryDescription(inventoryPart, relativePath);

        return directoryDescription;
    }

    public static FileDescription BuildFileDescription(InventoryPart inventoryPart, FileInfo fileInfo)
    {
        string relativePath;
        if (inventoryPart.InventoryPartType == FileSystemTypes.Directory)
        {
            relativePath = ExtractRelativePath(fileInfo.FullName, inventoryPart.RootPath);
        }
        else
        {
            relativePath = "/" + fileInfo.Name;
        }
            
        var fileDescription = new FileDescription(inventoryPart, relativePath);
        fileDescription.Size = fileInfo.Length;
        fileDescription.CreationTimeUtc = fileInfo.CreationTimeUtc;
        fileDescription.LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;

        return fileDescription;
    }
        
    private static string ExtractRelativePath(string fullName, string baseFullName)
    {
        string relativePath;
        var rawRelativePath = IOUtils.ExtractRelativePath(fullName, baseFullName);
        
        // We use '/' as a separator, whether on Windows, Linux or Mac
        // Because
        // - '/' is the separator on Linux and Mac
        // ' "\" is the separator on Windows
        // And that '/' is forbidden on Windows in file names
        // While "\" is allowed at least on Linux
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            relativePath = rawRelativePath.Replace(Path.DirectorySeparatorChar, '/');
        }
        else
        {
            relativePath = rawRelativePath;
        }

        if (!relativePath.StartsWith('/'))
        {
            relativePath = "/" + relativePath;
        }

        return relativePath;
    }
}