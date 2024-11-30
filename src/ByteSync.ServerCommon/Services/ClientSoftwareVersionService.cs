﻿using ByteSync.Common.Business.Auth;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Loaders;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services;

public class ClientSoftwareVersionService : IClientSoftwareVersionService
{
    private readonly IClientSoftwareVersionSettingsRepository _clientSoftwareVersionSettingsRepository;
    private readonly IClientSoftwareVersionSettingsLoader _clientSoftwareVersionSettingsLoader;
    private readonly ILogger<ClientSoftwareVersionService> _logger;
    private readonly AppSettings _appSettings;
    
    public ClientSoftwareVersionService(IClientSoftwareVersionSettingsRepository clientSoftwareVersionSettingsRepository,
        IClientSoftwareVersionSettingsLoader clientSoftwareVersionSettingsLoader, ILogger<ClientSoftwareVersionService> logger,
        IOptions<AppSettings> appSettings)
    {
        _clientSoftwareVersionSettingsRepository = clientSoftwareVersionSettingsRepository;
        _clientSoftwareVersionSettingsLoader = clientSoftwareVersionSettingsLoader;
        _logger = logger;
        _appSettings = appSettings.Value;
    }
    
    public async Task<ClientSoftwareVersionSettings?> GetClientSoftwareVersionSettings()
    {
        var clientSoftwareVersionSettings = await _clientSoftwareVersionSettingsRepository.GetUnique();
        
        if (clientSoftwareVersionSettings == null)
        {
            clientSoftwareVersionSettings = await _clientSoftwareVersionSettingsLoader.Load();
            
            await _clientSoftwareVersionSettingsRepository.SaveUnique(clientSoftwareVersionSettings);
        }
        
        return clientSoftwareVersionSettings;
    }

    public async Task<bool> IsClientVersionAllowed(LoginData loginData)
    {
        if (_appSettings.SkipClientsVersionCheck)
        {
            _logger.LogWarning("SkipClientsVersionCheck is set to true, skipping version check for client {ClientInstanceId} with version {Version}", 
                loginData.ClientInstanceId, loginData.Version);
            return true;
        }
        
        var clientSoftwareVersionSettings = await GetClientSoftwareVersionSettings();
                
        if (clientSoftwareVersionSettings?.MandatoryVersion == null)
        {
            return false;
        }

        var mandatory = new Version(clientSoftwareVersionSettings.MandatoryVersion.Version);
        var provided = new Version(loginData.Version);

        bool result = mandatory <= provided;
        
        if (!result)
        {
            _logger.LogWarning("Client version {Version} is not allowed, mandatory version is {MandatoryVersion}", loginData.Version, 
                clientSoftwareVersionSettings.MandatoryVersion.Version);
        }

        return result;
    }
}