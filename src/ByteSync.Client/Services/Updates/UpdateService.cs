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
                
            var applicableUpdates = new List<SoftwareVersion>();
            foreach (var softwareUpdate in updates)
            {
                var updateVersion = new Version(softwareUpdate.Version);

                if (updateVersion > _environmentService.ApplicationVersion)
                {
                    applicableUpdates.Add(softwareUpdate);
                }
            }

            var nextAvailableVersions = applicableUpdates
                .OrderBy(u => new Version(u.Version))
                .ThenBy(u => u.Level)
                .ToList();
            
            nextAvailableVersions = DeduplicateVersions(nextAvailableVersions);
            
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

    private List<SoftwareVersion> DeduplicateVersions(List<SoftwareVersion> nextAvailableVersions)
    {
        var deduplicatedVersions = new List<SoftwareVersion>();
        
        foreach (var softwareVersion in nextAvailableVersions)
        {
            if (deduplicatedVersions.All(v => v.Version != softwareVersion.Version))
            {
                deduplicatedVersions.Add(softwareVersion);
            }
        }
        
        return deduplicatedVersions;
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