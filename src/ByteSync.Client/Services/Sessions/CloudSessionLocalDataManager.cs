using System.IO;
using ByteSync.Business;
using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Services.Sessions;

public class CloudSessionLocalDataManager : ICloudSessionLocalDataManager
{
    private readonly ILocalApplicationDataManager _localApplicationDataManager;
    private readonly ISessionService _sessionService;
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly IEnvironmentService _environmentService;
    private readonly ILogger<CloudSessionLocalDataManager> _logger;

    public CloudSessionLocalDataManager(ILocalApplicationDataManager localApplicationDataManager,
        ISessionService cloudSessionDataHolder, ISessionMemberRepository sessionMemberRepository,
        IEnvironmentService environmentService, ILogger<CloudSessionLocalDataManager> logger)
    {
        _localApplicationDataManager = localApplicationDataManager;
        _sessionService = cloudSessionDataHolder;
        _sessionMemberRepository = sessionMemberRepository;
        _environmentService = environmentService;

        _logger = logger;
    }

    internal string GetSessionLocalPath()
    {
        var cloudSession = _sessionService.CurrentSession!;

        var sessionLocalPath = IOUtils.Combine(_localApplicationDataManager.ApplicationDataPath, "Sessions",
            $"{cloudSession.SessionId}_{cloudSession.Created:yyyyMMddTHHmmss}");

        Directory.CreateDirectory(sessionLocalPath);

        return sessionLocalPath;
    }

    private string GetMachineFullPath(ByteSyncEndpoint byteSyncEndpoint)
    {
        return GetMachineFullPath(byteSyncEndpoint.ClientInstanceId);
    }

    private string GetMachineFullPath(string clientInstanceId)
    {
        var sessionLocalPath = GetSessionLocalPath();
            
        string machinePath;
        if (Equals(clientInstanceId, _environmentService.ClientInstanceId))
        {
            machinePath = "thisMachine";
        }
        else
        {
            var sessionMember = _sessionMemberRepository.GetElement(clientInstanceId);
            machinePath = sessionMember!.MachineName;
        }
        machinePath += "_" + clientInstanceId;

        var machineFullPath = IOUtils.Combine(sessionLocalPath, machinePath);

        if (!Directory.Exists(machineFullPath))
        {
            Directory.CreateDirectory(machineFullPath);
        }

        return machineFullPath;
    }

    public string GetCurrentMachineInventoryPath(string letter, LocalInventoryModes localInventoryMode)
    {
        return GetInventoryPath(_environmentService.ClientInstanceId, letter, localInventoryMode);
    }

    public string GetInventoryPath(SharedFileDefinition sharedFileDefinition)
    {
        var sessionMemberInfo = _sessionMemberRepository.GetElement(sharedFileDefinition.ClientInstanceId);

        var localInventoryMode =
            sharedFileDefinition.SharedFileType == SharedFileTypes.BaseInventory
                ? LocalInventoryModes.Base
                : LocalInventoryModes.Full;
            
        return GetInventoryPath(sessionMemberInfo!.ClientInstanceId, sharedFileDefinition.AdditionalName, localInventoryMode);
    }

    public string GetInventoryPath(string clientInstanceId, string inventoryLetter, LocalInventoryModes localInventoryMode)
    {
        var machineFullPath = GetMachineFullPath(clientInstanceId);
        
        string fileName;
        if (localInventoryMode == LocalInventoryModes.Base)
        {
            fileName = $"base_inventory_{inventoryLetter}.zip";
        }
        else
        {
            fileName = $"full_inventory_{inventoryLetter}.zip";
        }
                
        var inventoryFullName = IOUtils.Combine(machineFullPath, fileName);

        return inventoryFullName;
    }

    public string GetFullPath(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var sessionMemberInfo = _sessionMemberRepository.GetElement(sharedFileDefinition.ClientInstanceId);

        if (sessionMemberInfo != null)
        {
            var machineFullPath = GetMachineFullPath(sessionMemberInfo.ClientInstanceId);

            var fileName = sharedFileDefinition.GetFileName(partNumber);

            var fullPath = IOUtils.Combine(machineFullPath, fileName);

            return fullPath;
        }
        else
        {
            return null;
        }
    }

    public string GetTempDeltaFullName(SharedDataPart source, SharedDataPart target)
    {
        var sessionLocalPath = GetSessionLocalPath();

        var tempFullPath = IOUtils.Combine(sessionLocalPath, "Temp");

        if (!Directory.Exists(tempFullPath))
        {
            Directory.CreateDirectory(tempFullPath);
        }

        var cpt = 0;
        string fullName;
        do
        {
            cpt += 1;
            var fileName = $"{source.SignatureGuid}_{target.SignatureGuid}_{cpt}.delta";

            fullName = IOUtils.Combine(tempFullPath, fileName);

        } while (File.Exists(fullName));

        return fullName;
    }

    public string GetSynchronizationStartDataPath()
    {
        var machineFullPath = GetMachineFullPath(_sessionMemberRepository.Elements.First().ClientInstanceId);
        
        var fileName = "synchronization_data.zip";
                
        var inventoryFullName = IOUtils.Combine(machineFullPath, fileName);

        return inventoryFullName;
    }

    public string GetSynchronizationTempZipPath(SharedFileDefinition sharedFileDefinition)
    {
        if (sharedFileDefinition.IsSynchronization)
        {
            var sessionLocalPath = GetSessionLocalPath();

            var tempFullPath = IOUtils.Combine(sessionLocalPath, "Temp");

            if (!Directory.Exists(tempFullPath))
            {
                Directory.CreateDirectory(tempFullPath);
            }
            
            var fileName = $"{sharedFileDefinition.Id}_temp.zip";

            var fullName = IOUtils.Combine(tempFullPath, fileName);

            return fullName;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(sharedFileDefinition), 
                "Unexpected SharedFileType: " + sharedFileDefinition.SharedFileType);
        }
    }

    public Task BackupCurrentSessionFiles()
    {
        return Task.Run(DoBackupCurrentSessionFiles);
    }

    /// <summary>
    /// The purpose of this method is to move files and directories from the session to a subdirectory called “previous_TICKS”.
    /// This is used at reset time, to move without deleting existing files
    /// Only other “previous_TICKS” folders are retained
    /// For the first backup, TICKS corresponds to the creation date of the “sessionLocalPath” directory
    /// For subsequent backups, TICKS corresponds to the creation date of the last “previous_TICKS” directory (with uniqueness management)
    /// </summary>
    private void DoBackupCurrentSessionFiles()
    {
        // We work in the session directory
        var sessionDirectory = new DirectoryInfo(GetSessionLocalPath());

        // previousDateTime will store the date used to generate “TICKS”
        DateTime previousDateTime;
        var existingPreviousDirectories = sessionDirectory.GetDirectories().Where(d => d.Name.StartsWith("previous_")).ToList();
        if (existingPreviousDirectories.Count > 0)
        {
            // If there are “previous_TICKS” directories, we take the most recent one
            var lastPrevious = existingPreviousDirectories.OrderBy(d => d.CreationTime).Last();
            previousDateTime = lastPrevious.CreationTime;
        }
        else
        {
            // No “previous_TICKS” directory
            previousDateTime = sessionDirectory.CreationTime;
        }

        // We generate the name “previous_TICKS”, with unicit management
        var previousDirectoryName = "previous_" + previousDateTime.Ticks;
        var previousDirectoryFullName = sessionDirectory.Combine(previousDirectoryName);
        var cpt = 0;
        while (Directory.Exists(previousDirectoryFullName))
        {
            cpt += 1;
            previousDirectoryFullName = sessionDirectory.Combine(previousDirectoryName) + "_" + cpt;
        }

        // We create the “previous_TICKS” directory, to which the current contents will be moved.
        var previousDirectory = new DirectoryInfo(previousDirectoryFullName);
        _logger.LogInformation("Creating backup directory {previousDirectory}", previousDirectory.FullName);
        previousDirectory.Create();

        // We move all directories except “previous_TICKS”
        foreach (var subDirectory in sessionDirectory.GetDirectories())
        {
            if (!subDirectory.Name.StartsWith("previous_"))
            {
                try
                {
                    _logger.LogInformation("Moving {subDirectory} to {destination}", subDirectory.FullName, previousDirectory.Name);
                    subDirectory.MoveTo(previousDirectory.Combine(subDirectory.Name));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while moving {directory}", subDirectory.FullName);
                }
            }
        }
        
        // Move all files
        foreach (var subFile in sessionDirectory.GetFiles())
        {
            try
            {
                _logger.LogInformation("Moving {subFile} to {destination}", subFile.FullName, previousDirectory.Name);
                subFile.MoveTo(previousDirectory.Combine(subFile.Name));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while moving {file}", subFile.FullName);
            }
        }
    }
}