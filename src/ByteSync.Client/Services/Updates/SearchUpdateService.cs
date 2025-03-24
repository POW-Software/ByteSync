using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Updates;

namespace ByteSync.Services.Updates;

public class SearchUpdateService : ISearchUpdateService
{
    private readonly IAvailableUpdatesLister _availableUpdatesLister;
    private readonly IEnvironmentService _environmentService;
    private readonly IAvailableUpdateRepository _availableUpdateRepository;
    private readonly ILogger<SearchUpdateService> _logger;

    public SearchUpdateService(IAvailableUpdatesLister availableUpdatesLister, IEnvironmentService environmentService,
        IAvailableUpdateRepository availableUpdateRepository, ILogger<SearchUpdateService> logger)
    {
        _availableUpdatesLister = availableUpdatesLister;
        _environmentService = environmentService;
        _availableUpdateRepository = availableUpdateRepository;
        _logger = logger;
    }
    
    public async Task SearchNextAvailableVersionsAsync()
    {
        try
        {
            if (IsApplicationInstalledFromStore)
            {
                _availableUpdateRepository.UpdateAvailableUpdates(new List<SoftwareVersion>());
                
                _logger.LogInformation("UpdateSystem: Application is installed from store, update check is disabled");
                
                return;
            }
            
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
                _logger.LogInformation("UpdateSystem: No available update found");
            }
            else
            {
                _logger.LogInformation("UpdateSystem: {count} available update(s) found", nextAvailableVersions.Count);

                foreach (var softwareVersion in nextAvailableVersions)
                {
                    _logger.LogInformation("UpdateSystem: - {version}, {level}", 
                        softwareVersion.Version, softwareVersion.Level);
                }
            }
            
            _availableUpdateRepository.UpdateAvailableUpdates(nextAvailableVersions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateSystem");
            
            _availableUpdateRepository.Clear();
        }
    }

    public bool IsApplicationInstalledFromStore
    {
        get
        {
            if (_environmentService.OSPlatform == OSPlatforms.Windows)
            {
                if (_environmentService.AssemblyFullName.Contains("\\Program Files\\WindowsApps\\")
                    || _environmentService.AssemblyFullName.Contains("\\Program Files (x86)\\WindowsApps\\"))
                {
                    return true;
                }
            }

            return false;
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
}