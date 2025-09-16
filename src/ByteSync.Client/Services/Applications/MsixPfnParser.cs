using System.IO;

namespace ByteSync.Services.Applications;

public class MsixPfnParser : IMsixPfnParser
{
    public bool TryParse(string containerDirectoryName, out string? packageFamilyName)
    {
        packageFamilyName = null;
        
        if (string.IsNullOrWhiteSpace(containerDirectoryName))
        {
            return false;
        }
        
        var idxUnderscore = containerDirectoryName.IndexOf('_');
        var idxDoubleUnderscore = containerDirectoryName.IndexOf("__", StringComparison.Ordinal);
        
        if (idxUnderscore > 0 && idxDoubleUnderscore > idxUnderscore)
        {
            var name = containerDirectoryName.Substring(0, idxUnderscore);
            var publisherId = containerDirectoryName.Substring(idxDoubleUnderscore + 2);
            
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(publisherId))
            {
                packageFamilyName = $"{name}_{publisherId}";
                
                return true;
            }
        }
        
        return false;
    }
    
    public bool TryDetectMsix(string applicationLauncherFullName, out string? msixPackageFamilyName)
    {
        msixPackageFamilyName = null;
        
        // MSIX container path detection (path-based)
        var normalized = applicationLauncherFullName.Replace('/', '\\');
        if (normalized.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase))
        {
            var exeDirectory = new FileInfo(normalized).Directory;
            var containerDirName = exeDirectory!.Name;
            
            if (TryParse(containerDirName, out var pfn))
            {
                msixPackageFamilyName = pfn;
            }
            
            return true;
        }
        
        return false;
    }
}