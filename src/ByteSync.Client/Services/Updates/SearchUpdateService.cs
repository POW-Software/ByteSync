﻿using ByteSync.Common.Business.Versions;
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
}