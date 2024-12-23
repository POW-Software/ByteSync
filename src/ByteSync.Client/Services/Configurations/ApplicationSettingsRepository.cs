﻿using System.Reflection;
using System.Security.Cryptography;
using ByteSync.Business.Configurations;
using ByteSync.Common.Business.Serials;
using ByteSync.Common.Helpers;
using ByteSync.Common.Interfaces;
using ByteSync.Interfaces;
using Serilog;

namespace ByteSync.Services.Configurations;

class ApplicationSettingsRepository : IApplicationSettingsRepository
{
    private readonly ILocalApplicationDataManager _localApplicationDataManager;
    private readonly IConfigurationReader<ApplicationSettings> _configurationReader;
    private readonly IConfigurationWriter<ApplicationSettings> _configurationWriter;
        
    private ProductSerialDescription? _productSerialDescription;
    private string? _encryptionPassword;

    public const string APPLICATION_SETTINGS_LAST_FORMAT_VERSION = "1.5";


    public ApplicationSettingsRepository(ILocalApplicationDataManager localApplicationDataManager, 
        IConfigurationReader<ApplicationSettings> configurationReader, IConfigurationWriter<ApplicationSettings> configurationWriter)
    {
        _localApplicationDataManager = localApplicationDataManager;
        _configurationReader = configurationReader;
        _configurationWriter = configurationWriter;

        SyncRoot = new object();
    }

    private object SyncRoot { get; }

    private ApplicationSettings? ApplicationSettings { get; set; }

    public string EncryptionPassword
    {
        get
        {
            if (_encryptionPassword.IsNullOrEmpty())
            {
                var encryptionPasswordBuilder = new EncryptionPasswordBuilder();
                _encryptionPassword = encryptionPasswordBuilder.Build();
            }

            return _encryptionPassword!;
        }
    }

    public ProductSerialDescription? ProductSerialDescription
    {
        get
        {
            lock (SyncRoot)
            {
                return _productSerialDescription;
            }
        }
        private set
        {
            lock (SyncRoot)
            {
                _productSerialDescription = value;
            }
        }
    }

    private string ApplicationSettingsPath
    {
        get
        {
            var configurationPath = IOUtils.Combine(_localApplicationDataManager.ApplicationDataPath, "ApplicationSettings.xml");

            return configurationPath;
        }
    }

    private void Initialize()
    {
        var hasTriedInitialization = false;
            
        try
        {
            // var configurationReader = new ConfigurationReader<ApplicationSettings>(ApplicationSettingsPath, true);

            var applicationSettings = _configurationReader.GetConfiguration(ApplicationSettingsPath);
            if (applicationSettings != null)
            {
                applicationSettings.SetEncryptionPassword(EncryptionPassword);

                // On contrôle que toutes les propriétés peuvent être lues sans lever d'exception
                CheckAllPropertiesCanBeRead(applicationSettings);
                    
                // on controle le format du fichier, pour mise à jour éventuelle
                var needUpdate = CheckFormatVersion(applicationSettings);
                
                Log.Information("Application Settings loaded from {configurationPath}", ApplicationSettingsPath);

                ApplicationSettings = applicationSettings;

                if (needUpdate)
                {
                    Log.Information("Application Settings has beed updated and need to be saved");
                        
                    SaveApplicationSettings();
                }
            }
            else
            {
                Log.Information("Application Settings not found in {configurationPath}", ApplicationSettingsPath);
                    
                hasTriedInitialization = true;
                InitializeApplicationSettings();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ApplicationSettingsManager: Error while loading or initializing Application Settings");

            if (!hasTriedInitialization)
            {
                try
                {
                    Log.Warning("Trying to initialize Application Settings");
                        
                    InitializeApplicationSettings();
                }
                catch (Exception ex2)
                {
                    Log.Error(ex2, "ApplicationSettingsManager: Error while initializing Application Settings");
                    Log.Warning("Application Settings will be set as default");
                        
                    // Tout a échoué, on load les valeurs par défaut
                    ApplicationSettings = new ApplicationSettings();
                }
            }
        }
    }

    private bool CheckFormatVersion(ApplicationSettings applicationSettings)
    {
        var needUpdate = false;
        
        if (!Equals(applicationSettings.SettingsVersion, APPLICATION_SETTINGS_LAST_FORMAT_VERSION))
        {
            var isRsaInitialized = false;
            if (applicationSettings.DecodedRsaPrivateKey == null || applicationSettings.DecodedRsaPrivateKey.Length == 0 ||
                applicationSettings.DecodedRsaPublicKey == null || applicationSettings.DecodedRsaPublicKey.Length == 0)
            {
                applicationSettings.InitializeRsa();
                isRsaInitialized = true;
            }
            
            if (applicationSettings.ClientId.IsNullOrEmpty())
            {
                applicationSettings.InitializeRsa();
                isRsaInitialized = true;
            }
            
            if (isRsaInitialized || applicationSettings.DecodedTrustedPublicKeys == null)
            {
                applicationSettings.InitializeTrustedPublicKeys();
            }
            
#pragma warning disable CS8604
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (applicationSettings.DecodedTrustedPublicKeys.Any(tpk => tpk.ClientId == null))
#pragma warning restore CS8604
            {
                applicationSettings.InitializeTrustedPublicKeys();
            }

            applicationSettings.SettingsVersion = APPLICATION_SETTINGS_LAST_FORMAT_VERSION;

            needUpdate = true;
        }
        
        if (applicationSettings.InstallationId.IsNullOrEmpty())
        {
            InitializeInstallationId(applicationSettings);
            needUpdate = true;
        }
        
        if (applicationSettings.ClientId.IsNullOrEmpty())
        {
            applicationSettings.InitializeRsa();
            applicationSettings.InitializeTrustedPublicKeys();
            needUpdate = true;
        }

        return needUpdate;
    }

    private void InitializeInstallationId(ApplicationSettings applicationSettings)
    {
        applicationSettings.InstallationId = $"InstallationId_{Guid.NewGuid()}";
    }
    
    private void CheckAllPropertiesCanBeRead(ApplicationSettings applicationSettings)
    {
        var properties = typeof(ApplicationSettings)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var propertyInfo in properties)
        {
            var _ = propertyInfo.GetValue(applicationSettings);
        }
            
        CheckRsa(applicationSettings);
    }

    private static void CheckRsa(ApplicationSettings applicationSettings)
    {
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(applicationSettings.DecodedRsaPublicKey, out _);

        rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(applicationSettings.DecodedRsaPrivateKey, out _);
    }

    private void InitializeApplicationSettings()
    {
        Log.Information("Initializing Application Settings");
            
        var applicationSettings = PrepareApplicationSettings();

        ApplicationSettings = applicationSettings;

        SaveApplicationSettings();
    }

    private ApplicationSettings PrepareApplicationSettings()
    {
        var applicationSettings = new ApplicationSettings();
        applicationSettings.SetEncryptionPassword(EncryptionPassword);

        // ReSharper disable RedundantAssignment
        var email = "";
        var serial = "";
        // ReSharper restore RedundantAssignment
            
    #if DEBUG
        email = "paul.fresquet@pow-software.com";
        serial = "BS-ADM-DEBUG";
    #endif

        applicationSettings.DecodedEmail = email;
        applicationSettings.DecodedSerial = serial;
            
        InitializeInstallationId(applicationSettings);
        
        applicationSettings.InitializeRsa();
        applicationSettings.InitializeTrustedPublicKeys();
            
        CheckRsa(applicationSettings);

        applicationSettings.AgreesBetaWarning0 = false;

        applicationSettings.SettingsVersion = APPLICATION_SETTINGS_LAST_FORMAT_VERSION;
            
        return applicationSettings;
    }

    public ApplicationSettings GetCurrentApplicationSettings()
    {
        lock (SyncRoot)
        {
            if (ApplicationSettings == null)
            {
                Initialize();
            }
            
            return (ApplicationSettings) ApplicationSettings!.Clone();
        }
    }

    public ApplicationSettings UpdateCurrentApplicationSettings(Action<ApplicationSettings> handler, bool saveAfter)
    {
        lock (SyncRoot)
        {
            handler.Invoke(ApplicationSettings!);

            if (saveAfter)
            {
                SaveApplicationSettings();
            }
            
            return (ApplicationSettings) ApplicationSettings!.Clone();
        }
    }

    private void SaveApplicationSettings()
    {
        lock (SyncRoot)
        {
            var applicationSettings = (ApplicationSettings)ApplicationSettings!.Clone();
            
            applicationSettings.SetEncryptionPassword(EncryptionPassword);

            try
            {
                _configurationWriter.SaveConfiguration(applicationSettings, ApplicationSettingsPath);

                Log.Information("Application Settings saved to {ApplicationSettingsPath}", ApplicationSettingsPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ApplicationSettingsManager: can not save user settings");
            }
        }
    }
}