using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Applications;

namespace ByteSync.Helpers;

public static class EnvironmentServiceExtensions
{
    public static bool IsInstalledFromWindowsStore(this IEnvironmentService environmentService)
    {
        if (environmentService.OSPlatform == OSPlatforms.Windows)
        {
            if (environmentService.AssemblyFullName.Contains("\\Program Files\\WindowsApps\\") ||
                environmentService.AssemblyFullName.Contains("\\Program Files (x86)\\WindowsApps\\"))
            {
                return true;
            }
        }

        return false;
    }
}
