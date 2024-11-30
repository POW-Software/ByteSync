using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ByteSync.Business.Profiles;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Profiles;
using Newtonsoft.Json;
using Serilog;

namespace ByteSync.Services.Profiles;

public class SessionProfileLocalDataManager : ISessionProfileLocalDataManager
{
    private readonly ILocalApplicationDataManager _localApplicationDataManager;
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;

    public SessionProfileLocalDataManager(ILocalApplicationDataManager localApplicationDataManager, IApplicationSettingsRepository applicationSettingsManager)
    {
        _localApplicationDataManager = localApplicationDataManager;
        _applicationSettingsRepository = applicationSettingsManager;
    }
    
    public string GetProfileZipPath(string profileId)
    {
        var profilesDirectory = IOUtils.Combine(_localApplicationDataManager.ApplicationDataPath, "Profiles", profileId);
        Directory.CreateDirectory(profilesDirectory);
        
        var profileLocalPath = IOUtils.Combine(profilesDirectory, $"{profileId}.zip");

        return profileLocalPath;
    }

    public string GetProfileZipPath(SharedFileDefinition sharedFileDefinition)
    {
        if (sharedFileDefinition.IsProfileDetails)
        {
            var profilesDirectory = IOUtils.Combine(_localApplicationDataManager.ApplicationDataPath, "Profiles", sharedFileDefinition.AdditionalName);
            Directory.CreateDirectory(profilesDirectory);
            
            var profileLocalPath = IOUtils.Combine(profilesDirectory, $"{sharedFileDefinition.AdditionalName}.zip");

            return profileLocalPath;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(sharedFileDefinition), 
                "Unexpected SharedFileType: " + sharedFileDefinition.SharedFileType);
        }
    }

    public DirectoryInfo GetProfileDirectory(AbstractSessionProfile sessionProfile)
    {
        var profileDirectory = IOUtils.Combine(_localApplicationDataManager.ApplicationDataPath, "Profiles", sessionProfile.ProfileId);

        return new DirectoryInfo(profileDirectory);
    }
    
    public DirectoryInfo GetProfileDirectory(AbstrastSessionProfileDetails abstrastSessionProfileDetails)
    {
        var profileDirectory = IOUtils.Combine(_localApplicationDataManager.ApplicationDataPath, "Profiles", abstrastSessionProfileDetails.ProfileId);

        return new DirectoryInfo(profileDirectory);
    }


    public Task<List<AbstractSessionProfile>> GetAllSavedProfiles()
    {
        return Task.Run(() =>
        {
            var result = new List<AbstractSessionProfile>();
            
            var profilesDirectoryPath = IOUtils.Combine(_localApplicationDataManager.ApplicationDataPath, "Profiles");
            var profilesDirectory = new DirectoryInfo(profilesDirectoryPath);

            if (profilesDirectory.Exists)
            {
                // Les profils sont rangés dans un sous-répertoire
                foreach (var subDirectory in profilesDirectory.GetDirectories())
                {
                    if (!subDirectory.Name.StartsWith("CSP", StringComparison.InvariantCultureIgnoreCase)
                        && !subDirectory.Name.StartsWith("LSP", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Log.Warning("The name of Directory {Path} is not expected. It will be ignored", subDirectory.FullName);

                        continue;
                    }
                    
                    foreach (var fileInfo in subDirectory.GetFiles())
                    {
                        if (fileInfo.Name.StartsWith("CSP", StringComparison.InvariantCultureIgnoreCase)
                            && fileInfo.Name.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var cloudSessionProfile = LoadProfile<CloudSessionProfile>(fileInfo, subDirectory);

                            if (cloudSessionProfile != null)
                            {
                                result.Add(cloudSessionProfile);
                            }
                        }
                        else if (fileInfo.Name.StartsWith("LSP", StringComparison.InvariantCultureIgnoreCase)
                                 && fileInfo.Name.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var localSessionProfile = LoadProfile<LocalSessionProfile>(fileInfo, subDirectory);
                        
                            if (localSessionProfile != null)
                            {
                                result.Add(localSessionProfile);
                            }
                        }
                    }
                }
            }

            return result;
        });
    }

    public async Task<CloudSessionProfileDetails?> LoadCloudSessionProfileDetails(CloudSessionProfile cloudSessionProfile)
    {
        var joinLobbyParameters = new GetProfileDetailsPasswordParameters();
        joinLobbyParameters.CloudSessionProfileId = cloudSessionProfile.ProfileId;
        joinLobbyParameters.ProfileClientId = cloudSessionProfile.ProfileClientId;
        
        var profileDetailsPassword = "temp_refactoring";

        if (profileDetailsPassword != null)
        {
            return await LoadCloudSessionProfileDetails(cloudSessionProfile.ProfileId, profileDetailsPassword);
        }
        else
        {
            return null;
        }
    }
    
    public Task<CloudSessionProfileDetails> LoadCloudSessionProfileDetails(LobbyInfo lobbyInfo)
    {
        return LoadCloudSessionProfileDetails(lobbyInfo.CloudSessionProfileId, lobbyInfo.ProfileDetailsPassword);
    }

    private Task<CloudSessionProfileDetails> LoadCloudSessionProfileDetails(string cloudSessionProfileId, string profileDetailsPassword)
    {
        return Task.Run(() =>
        {
            var profilePath = GetProfileZipPath(cloudSessionProfileId);
        
            using var zipArchive = ZipFile.Open(profilePath, ZipArchiveMode.Read);
        
            var detailsAFile = zipArchive.GetEntry("details_B.json");
            using var detailsAStream = detailsAFile!.Open();
            using var detailsAStreamReader = new StreamReader(detailsAStream);
            var detailsAContent = detailsAStreamReader.ReadToEnd();

            var decrypted1 = CryptographyUtils.Decrypt(detailsAContent, _applicationSettingsRepository.EncryptionPassword);
            var decrypted2 = CryptographyUtils.Decrypt(decrypted1, profileDetailsPassword);

            var cloudSessionProfileDetails = JsonConvert.DeserializeObject<CloudSessionProfileDetails>(decrypted2);

            return cloudSessionProfileDetails!;
        });
    }
    
    public Task<LocalSessionProfileDetails> LoadLocalSessionProfileDetails(LocalSessionProfile localSessionProfile)
    {
        return Task.Run(() =>
        {
            var profilePath = GetProfileZipPath(localSessionProfile.ProfileId);

            using var zipArchive = ZipFile.Open(profilePath, ZipArchiveMode.Read);

            var detailsAFile = zipArchive.GetEntry("details_A.json");
            using var detailsAStream = detailsAFile!.Open();
            using var detailsAStreamReader = new StreamReader(detailsAStream);
            var detailsAContent = detailsAStreamReader.ReadToEnd();

            var decrypted1 = CryptographyUtils.Decrypt(detailsAContent, _applicationSettingsRepository.EncryptionPassword);

            var localSessionProfileDetails = JsonConvert.DeserializeObject<LocalSessionProfileDetails>(decrypted1);

            return localSessionProfileDetails!;
        });
    }

    public void DeleteSessionProfile(AbstractSessionProfile sessionProfile)
    {
        var directoryInfo = GetProfileDirectory(sessionProfile);
        
        DoDeleteProfile(directoryInfo);
    }

    public void DeleteSessionProfile(AbstrastSessionProfileDetails cloudSessionProfileDetails)
    {
        var directoryInfo = GetProfileDirectory(cloudSessionProfileDetails);

        DoDeleteProfile(directoryInfo);
    }

    private static void DoDeleteProfile(DirectoryInfo directoryInfo)
    {
        Log.Information("Deleting Profile Directory {Directory}", directoryInfo.FullName);

        directoryInfo.Delete(true);
    }

    private T? LoadProfile<T>(FileInfo fileInfo, DirectoryInfo directoryInfo) where T : AbstractSessionProfile
    {
        try
        {
            using var zipArchive = ZipFile.Open(fileInfo.FullName, ZipArchiveMode.Read);

            var infoFile = zipArchive.GetEntry("info.json");

            if (infoFile == null)
            {
                Log.Warning("The Session Profile at {Path} is not well-formed. It will be ignored", fileInfo.FullName);
                return null;
            }
            
            using var infoStream = infoFile!.Open();
            using var infoStreamReader = new StreamReader(infoStream);
            
            var content = infoStreamReader.ReadToEnd();
            
            var sessionProfile = JsonConvert.DeserializeObject<T>(content);
            
            // var lastRunInfo = directoryInfo.GetFiles().

            return sessionProfile;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Can not load the Session Profile from {Path}", fileInfo.FullName);

            return null;
        }
    }
}