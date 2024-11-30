using System.IO;
using System.Runtime.InteropServices;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Updates;

namespace ByteSync.Services.Updates;

public class UpdateHelperService : IUpdateHelperService
{
    private readonly IEnvironmentService _environmentService;

    public UpdateHelperService(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
    }

    public DirectoryInfo? GetApplicationBaseDirectory()
    {
        var applicationLauncher = new FileInfo(_environmentService.AssemblyFullName);

        DirectoryInfo? applicationBaseDirectory = null;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            DirectoryInfo? macOsDir = null;
            if (applicationLauncher.Directory != null && 
                applicationLauncher.Directory.Name.Equals("MacOS", StringComparison.InvariantCultureIgnoreCase))
            {
                macOsDir = applicationLauncher.Directory;
            }
            else if (applicationLauncher.Directory?.Parent != null
                     && applicationLauncher.Directory.Parent.Name.Equals("MacOS", StringComparison.InvariantCultureIgnoreCase))
            {
                macOsDir = applicationLauncher.Directory.Parent;
            }

            if (macOsDir?.Parent != null &&
                macOsDir.Parent.Name.Equals("Contents", StringComparison.InvariantCultureIgnoreCase) &&
                macOsDir.Parent.Parent != null &&
                macOsDir.Parent.Parent.Name.StartsWith("ByteSync", StringComparison.InvariantCultureIgnoreCase) &&
                macOsDir.Parent.Parent.Name.EndsWith(".app", StringComparison.InvariantCultureIgnoreCase))
            {
                applicationBaseDirectory = macOsDir.Parent.Parent;
            }
        }
        else
        {
            applicationBaseDirectory = applicationLauncher.Directory;
        }

        return applicationBaseDirectory;
    }
}