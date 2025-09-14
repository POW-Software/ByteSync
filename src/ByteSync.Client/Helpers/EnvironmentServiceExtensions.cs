using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Applications;

namespace ByteSync.Helpers;

public static class EnvironmentServiceExtensions
{
    public static bool IsInstalledFromWindowsStore(this IEnvironmentService environmentService)
    {
        return environmentService.DeploymentMode == DeploymentMode.MsixInstallation;
    }
}