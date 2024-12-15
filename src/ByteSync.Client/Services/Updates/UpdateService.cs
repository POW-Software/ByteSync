using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Updates;
using DynamicData;

namespace ByteSync.Services.Updates;

class UpdateService : IUpdateService
{
    private readonly IAvailableUpdatesLister _availableUpdatesLister;
    private readonly IEnvironmentService _environmentService;
    private readonly IApplyUpdateService _applyUpdateService;
    private readonly IAvailableUpdateRepository _availableUpdateRepository;
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(IAvailableUpdatesLister availableUpdatesLister, IEnvironmentService environmentService, IApplyUpdateService applyUpdateService,
        IAvailableUpdateRepository availableUpdateRepository, ILogger<UpdateService> logger)
    {
        _availableUpdatesLister = availableUpdatesLister;
        _environmentService = environmentService;
        _applyUpdateService = applyUpdateService;
        _availableUpdateRepository = availableUpdateRepository;
        _logger = logger;
    }

    public async Task SearchNextAvailableVersionsAsync()
    {
        try
        {
            var updates = await _availableUpdatesLister.GetAvailableUpdates();
                
            List<SoftwareVersion> applicableUpdates = new List<SoftwareVersion>();
            foreach (var softwareUpdate in updates)
            {
                Version updateVersion = new Version(softwareUpdate.Version);

                if (updateVersion > _environmentService.ApplicationVersion)
                {
                    applicableUpdates.Add(softwareUpdate);
                }
            }

            var nextAvailableVersions = applicableUpdates.OrderBy(u => new Version(u.Version)).ToList();
            
            if (nextAvailableVersions.Count == 0)
            {
                _logger.LogInformation("UpdateManager.GetNextAvailableVersions: no available update found");
            }
            else
            {
                _logger.LogInformation("UpdateManager.GetNextAvailableVersions: {count} available update(s) found", nextAvailableVersions.Count);

                foreach (var softwareVersion in nextAvailableVersions)
                {
                    _logger.LogInformation("UpdateManager.GetNextAvailableVersions: - {version}, {level}", 
                        softwareVersion.Version, softwareVersion.Level);
                }
            }
            
            _availableUpdateRepository.UpdateAvailableUpdates(nextAvailableVersions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateManager.GetNextAvailableVersions");
            
            _availableUpdateRepository.Clear();
        }
    }

    public async Task<bool> UpdateAsync(SoftwareVersion softwareVersion, CancellationToken cancellationToken)
    {
        var softwareFileVersion = GuessSoftwareFileVersion(softwareVersion);
        
        return await _applyUpdateService.Update(softwareVersion, softwareFileVersion, cancellationToken);
    }

    private SoftwareVersionFile GuessSoftwareFileVersion(SoftwareVersion softwareVersion)
    {
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        bool isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

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

    // private void UpdateCache(List<SoftwareVersion> nextAvailableVersions)
    // {
    //     foreach (SoftwareVersion softwareVersion in nextAvailableVersions)
    //     {
    //         NextVersionsCache.AddOrUpdate(softwareVersion);
    //     }
    //
    //     var currentKeys = NextVersionsCache.Keys.ToList();
    //
    //     foreach (var key in currentKeys)
    //     {
    //         var itemInUpdateCollection = nextAvailableVersions.FirstOrDefault(item => item.Version.Equals(key));
    //
    //         if (itemInUpdateCollection == null)
    //         {
    //             NextVersionsCache.RemoveKey(key);
    //         }
    //     }
    // }
}