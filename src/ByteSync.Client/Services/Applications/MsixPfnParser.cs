using System.IO;
using ByteSync.Common.Business.Misc;

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
    
    public bool TryDetect(string applicationLauncherFullName,
        IEnumerable<string> programsDirectoriesCandidates,
        out DeploymentModes deploymentMode,
        out string? msixPackageFamilyName)
    {
        msixPackageFamilyName = null;
        
        // MSIX container path detection (path-based, not OS-gated)
        var normalized = applicationLauncherFullName.Replace('/', '\\');
        if (normalized.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase))
        {
            var exeDirectory = new FileInfo(normalized).Directory;
            var containerDirName = exeDirectory!.Name;
            
            if (TryParse(containerDirName, out var pfn))
            {
                msixPackageFamilyName = pfn;
            }
            
            deploymentMode = DeploymentModes.MsixInstallation;
            
            return true;
        }
        
        var installedInPrograms = false;
        foreach (var candidate in programsDirectoriesCandidates)
        {
            if (IOUtils.IsSubPathOf(applicationLauncherFullName, candidate))
            {
                installedInPrograms = true;
                
                break;
            }
        }
        
        if (applicationLauncherFullName.Contains("/homebrew/", StringComparison.OrdinalIgnoreCase) ||
            applicationLauncherFullName.Contains("/linuxbrew/", StringComparison.OrdinalIgnoreCase))
        {
            installedInPrograms = true;
        }
        
        deploymentMode = installedInPrograms ? DeploymentModes.SetupInstallation : DeploymentModes.Portable;
        
        return true;
    }
}