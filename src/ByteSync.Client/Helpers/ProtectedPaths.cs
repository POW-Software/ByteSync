using System.IO;
using ByteSync.Common.Business.Misc;

namespace ByteSync.Helpers;

public static class ProtectedPaths
{
    private static readonly string[] _protectedRoots =
    [
        "/dev",
        "/proc",
        "/sys",
        "/run",
        "/var/run",
        "/private/var/run"
    ];
    
    public static bool TryGetProtectedRoot(string path, OSPlatforms osPlatform, out string protectedRoot)
    {
        protectedRoot = string.Empty;
        
        if (osPlatform == OSPlatforms.Windows)
        {
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }
        
        var normalizedPath = Normalize(path);
        
        foreach (var root in _protectedRoots)
        {
            var normalizedRoot = Normalize(root);
            
            if (IsSameOrSubPath(normalizedPath, normalizedRoot, StringComparison.Ordinal))
            {
                protectedRoot = root;
                
                return true;
            }
        }
        
        return false;
    }
    
    private static string Normalize(string path)
    {
        var fullPath = Path.GetFullPath(path);
        
        return Path.TrimEndingDirectorySeparator(fullPath);
    }
    
    private static bool IsSameOrSubPath(string path, string root, StringComparison comparison)
    {
        if (path.Equals(root, comparison))
        {
            return true;
        }
        
        var rootWithSeparator = root + Path.DirectorySeparatorChar;
        
        return path.StartsWith(rootWithSeparator, comparison);
    }
}