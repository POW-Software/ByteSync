using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Updates;

namespace ByteSync.Services.Updates;

class UpdateService : IUpdateService
{
    private readonly IApplyUpdateService _applyUpdateService;

    public UpdateService(IApplyUpdateService applyUpdateService)
    {
        _applyUpdateService = applyUpdateService;
    }

    public async Task<bool> UpdateAsync(SoftwareVersion softwareVersion, CancellationToken cancellationToken)
    {
        var softwareFileVersion = GuessSoftwareFileVersion(softwareVersion);
        
        return await _applyUpdateService.Update(softwareVersion, softwareFileVersion, cancellationToken);
    }

    private SoftwareVersionFile GuessSoftwareFileVersion(SoftwareVersion softwareVersion)
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        var isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        SoftwareVersionFile softwareFileVersion;
        if (isWindows)
        {
            softwareFileVersion = softwareVersion.Files.Single(f => f.Platform == Platform.Windows);
        }
        else if (isLinux)
        {
            softwareFileVersion = softwareVersion.Files.Single(f => f.Platform == Platform.Linux);
        }
        else if (isOsx)
        {
            softwareFileVersion = softwareVersion.Files.Single(f => f.Platform == Platform.Osx);
        }
        else
        {
            throw new Exception("unable to detect OS Platform");
        }

        return softwareFileVersion;
    }
}