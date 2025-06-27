using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Business.Communications;
using ByteSync.Business.Profiles;
using ByteSync.Common.Business.Profiles;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Controls.Json;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Profiles;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Profiles;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Services.Misc;
using ByteSync.Services.Sessions;
using Serilog;

namespace ByteSync.Services.Profiles;

public class SessionProfileManager : ISessionProfileManager
{
    private readonly ISessionProfileLocalDataManager _sessionProfileLocalDataManager;
    private readonly IDialogService _dialogService;
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly IDataSourceService _dataSourceService;
    private readonly ISynchronizationRuleRepository _synchronizationRuleRepository;
    private readonly IEnvironmentService _environmentService;
    private readonly IConnectionService _connectionService;
    private readonly ICloudSessionProfileApiClient _cloudSessionProfileApiClient;
    private readonly IFileUploaderFactory _fileUploaderFactory;
    private readonly ISynchronizationRulesConverter _synchronizationRulesConverter;
    private readonly IDataSourceRepository _dataSourceRepository;

    public SessionProfileManager(ISessionProfileLocalDataManager sessionProfileLocalDataManager, IDialogService dialogService, 
        IApplicationSettingsRepository applicationSettingsManager, ISessionMemberRepository sessionMemberRepository, 
        IDataSourceService dataSourceService, ISynchronizationRuleRepository synchronizationRuleRepository, 
        IEnvironmentService environmentService, IConnectionService connectionService, ICloudSessionProfileApiClient cloudSessionProfileApiClient,
        IFileUploaderFactory fileUploaderFactory, ISynchronizationRulesConverter synchronizationRulesConverter,
        IDataSourceRepository dataSourceRepository)
    {
        _sessionProfileLocalDataManager = sessionProfileLocalDataManager;
        _dialogService = dialogService;
        _applicationSettingsRepository = applicationSettingsManager;
        _sessionMemberRepository = sessionMemberRepository;
        _dataSourceService = dataSourceService;
        _synchronizationRuleRepository = synchronizationRuleRepository;
        _environmentService = environmentService;
        _connectionService = connectionService;
        _cloudSessionProfileApiClient = cloudSessionProfileApiClient;
        _fileUploaderFactory = fileUploaderFactory;
        _synchronizationRulesConverter = synchronizationRulesConverter;
        _dataSourceRepository = dataSourceRepository;
    }
    
    public async Task CreateCloudSessionProfile(string sessionId, string profileName, CloudSessionProfileOptions cloudSessionProfileOptions)
    {
        try
        {
            var createCloudSessionProfileResult = await _cloudSessionProfileApiClient.CreateCloudSessionProfile(sessionId);

            if (createCloudSessionProfileResult.IsOK)
            {
                var cloudSessionProfileDetails = BuildCloudSessionProfileDetails(createCloudSessionProfileResult.Data!, 
                    profileName, cloudSessionProfileOptions);
                
                // On crée le SFD
                var sharedFileDefinition = new SharedFileDefinition();
                sharedFileDefinition.SharedFileType = SharedFileTypes.ProfileDetails;
                sharedFileDefinition.ClientInstanceId = _connectionService.ClientInstanceId!;
                sharedFileDefinition.SessionId = sessionId;
                sharedFileDefinition.AdditionalName = cloudSessionProfileDetails.CloudSessionProfileId;
                
                var profileDetailsPath = _sessionProfileLocalDataManager.GetProfileZipPath(cloudSessionProfileDetails.CloudSessionProfileId);
                
                var localSharedFile = new LocalSharedFile(sharedFileDefinition, profileDetailsPath);
                
                // On save le fichier
                SaveToFile(cloudSessionProfileDetails, createCloudSessionProfileResult.Data!.ProfileDetailsPassword, localSharedFile.LocalPath);

                var fileUploader = _fileUploaderFactory.Build(localSharedFile.LocalPath, sharedFileDefinition);
                // var fileUploader = _sessionObjectsFactory.BuildFileUploader();
                await fileUploader.Upload();
                
                // Après upload, on complète le fichier
                var cloudSessionProfile = BuildCloudSessionProfileInfo(cloudSessionProfileDetails, createCloudSessionProfileResult.Data);

                UpdateFile(cloudSessionProfile, localSharedFile.LocalPath);

                Log.Information("Created Cloud Session Profile {CloudSessionProfileId}", cloudSessionProfileDetails.CloudSessionProfileId);
            }
            else
            {
                Log.Information("Can not create Cloud Session Profile");
            }
        }
        finally
        {

        }
    }

    public async Task CreateLocalSessionProfile(string sessionId, string profileName, LocalSessionProfileOptions localSessionProfileOptions)
    {
        try
        {
            var localSessionProfileDetails = BuildLocalSessionProfileDetails(profileName, localSessionProfileOptions);

            var profileDetailsPath = _sessionProfileLocalDataManager.GetProfileZipPath(localSessionProfileDetails.LocalSessionProfileId);
            
            var localSessionProfile = BuildLocalSessionProfileInfo(localSessionProfileDetails);
            
            // On save le fichier
            SaveToFile(localSessionProfileDetails, localSessionProfile, profileDetailsPath);

            Log.Information("Created Local Session Profile {LocalSessionProfileId}", localSessionProfileDetails.LocalSessionProfileId);
        }
        finally
        {

        }
    }

    public async Task OnFileIsFullyDownloaded(LocalSharedFile localSharedFile)
    {
        if (localSharedFile.SharedFileDefinition.IsProfileDetails)
        {
            var cloudSessionProfileData = await _cloudSessionProfileApiClient.GetCloudSessionProfileData(localSharedFile.SharedFileDefinition.SessionId, 
                localSharedFile.SharedFileDefinition.AdditionalName);
            
            var profileDetailsPath = _sessionProfileLocalDataManager.GetProfileZipPath(localSharedFile.SharedFileDefinition);

            // on fait un check extraction, puis on update le fichier
            var cloudSessionProfileDetails = CheckExtractProfileDetails(profileDetailsPath, cloudSessionProfileData);
            if (cloudSessionProfileDetails != null)
            {
                // on demande à l'utilisateur s'il veut enregistrer le profil
                var messageBoxViewModel = _dialogService.CreateMessageBoxViewModel(
                    nameof(Resources.SessionProfileManager_OnReceivedCloudSessionProfile_Title), 
                    nameof(Resources.SessionProfileManager_OnReceivedCloudSessionProfile_Message),
                    cloudSessionProfileDetails.Name);
                messageBoxViewModel.ShowYesNo = true;
                var result = await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);

                if (result == MessageBoxResult.No)
                {
                    Log.Information("Current user refused to save received Cloud Session Profile {CloudSessionProfileId}, " +
                                    "Deleting received Cloud Session Profile", 
                        cloudSessionProfileDetails.CloudSessionProfileId);

                    _sessionProfileLocalDataManager.DeleteSessionProfile(cloudSessionProfileDetails);
                    
                    // On doit supprimer le fichier reçu
                    // File.Delete(profileDetailsPath);

                    return;
                }
                
                var cloudSessionProfile = BuildCloudSessionProfileInfo(cloudSessionProfileDetails, cloudSessionProfileData);
                
                UpdateFile(cloudSessionProfile, profileDetailsPath);
                
                Log.Information("Saved received Cloud Session Profile {CloudSessionProfileId}", cloudSessionProfileDetails.CloudSessionProfileId);
            }
            else
            {
                Log.Warning("Can not save received Cloud Session Profile {CloudSessionProfileId}", localSharedFile.SharedFileDefinition.AdditionalName);
            }
        }
    }

    public async Task<bool> DeleteSessionProfile(AbstractSessionProfile sessionProfile)
    {
        if (sessionProfile is CloudSessionProfile cloudSessionProfile)
        {
            try
            {
                // Suppression sur le serveur
                var parameters = new DeleteCloudSessionProfileParameters();
                parameters.CloudSessionProfileId = cloudSessionProfile.ProfileId;
                parameters.ProfileClientId = cloudSessionProfile.ProfileClientId;

                var isDeleted = await _cloudSessionProfileApiClient.DeleteCloudSessionProfile(parameters);

                if (isDeleted)
                {
                    Log.Information("Successfully deleted Cloud Session Profile {ProfileName} on the server", sessionProfile.Name);
                }
                else
                {
                    Log.Information("Cloud Session Profile {ProfileName} cannot be deleted on the server. It has probably already been deleted", 
                        sessionProfile.Name);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while deleting Cloud Session Profile {ProfileName} on the server", sessionProfile.Name);
            }
        }

        
        try
        {
            // Suppression locale
            _sessionProfileLocalDataManager.DeleteSessionProfile(sessionProfile);

            Log.Information("Successfully deleted Session Profile {ProfileName} on this machine", sessionProfile.Name);

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while deleting Session Profile {ProfileName} on this machine", sessionProfile.Name);
        }

        return false;
    }

    private CloudSessionProfile BuildCloudSessionProfileInfo(CloudSessionProfileDetails cloudSessionProfileDetails,
        CloudSessionProfileData cloudSessionProfileData)
    {
        var cloudSessionProfile = new CloudSessionProfile();

        cloudSessionProfile.Name = cloudSessionProfileDetails.Name;
        cloudSessionProfile.ProfileId = cloudSessionProfileDetails.CloudSessionProfileId;
        cloudSessionProfile.CreationDatetime = cloudSessionProfileDetails.CreationDatetime;
        cloudSessionProfile.ProfileClientId = cloudSessionProfileData.RequesterProfileClientId;
        cloudSessionProfile.MembersProfileClientIds = cloudSessionProfileData.Slots.Select(t => t.ProfileClientId).ToList();

        cloudSessionProfile.CreatedWithVersion = _environmentService.ApplicationVersion.ToString();

        return cloudSessionProfile;
    }
    
    private LocalSessionProfile BuildLocalSessionProfileInfo(LocalSessionProfileDetails localSessionProfileDetails)
    {
        var localSessionProfile = new LocalSessionProfile();

        localSessionProfile.Name = localSessionProfileDetails.Name;
        localSessionProfile.ProfileId = localSessionProfileDetails.LocalSessionProfileId;
        localSessionProfile.CreationDatetime = localSessionProfileDetails.CreationDatetime;
        
        localSessionProfile.CreatedWithVersion = _environmentService.ApplicationVersion.ToString();

        return localSessionProfile;
    }

    private CloudSessionProfileDetails? CheckExtractProfileDetails(string profileDetailsPath, CloudSessionProfileData cloudSessionProfileData)
    {
        using var zipArchive = ZipFile.Open(profileDetailsPath, ZipArchiveMode.Read);
        
        var detailsAFile = zipArchive.GetEntry("details_A.json");
        using var detailsAStream = detailsAFile!.Open();
        using var detailsAStreamReader = new StreamReader(detailsAStream);
        var detailsAContent = detailsAStreamReader.ReadToEnd();
        var decrypted = CryptographyUtils.Decrypt(detailsAContent, cloudSessionProfileData.ProfileDetailsPassword);

        var cloudSessionProfileDetails = JsonHelper.Deserialize<CloudSessionProfileDetails>(decrypted);

        var isOK = cloudSessionProfileDetails.CloudSessionProfileId.Equals(cloudSessionProfileData.CloudSessionProfileId);

        if (isOK)
        {
            return cloudSessionProfileDetails;
        }
        else
        {
            return null;
        }
    }

    private void UpdateFile(CloudSessionProfile cloudSessionProfile, string localPath)
    {
        using var zipArchive = ZipFile.Open(localPath, ZipArchiveMode.Update);
        
        // Gestion du fichier info
        var json = JsonHelper.Serialize(cloudSessionProfile);
        var infoFile = zipArchive.CreateEntry("info.json");
        using var entryStream = infoFile.Open();
        using var streamWriter = new StreamWriter(entryStream);
        streamWriter.Write(json);
        
        // Chiffrement supplémentaire du fichier details
        var detailsAFile = zipArchive.GetEntry("details_A.json");
        using var detailsAStream = detailsAFile!.Open();
        using var detailsAStreamReader = new StreamReader(detailsAStream);
        var detailsAContent = detailsAStreamReader.ReadToEnd();
        var encrypted = CryptographyUtils.Encrypt(detailsAContent, _applicationSettingsRepository.EncryptionPassword);
        var detailsBFile = zipArchive.CreateEntry("details_B.json");
        using var detailsBStream = detailsBFile.Open();
        using var detailsBSteamWriter = new StreamWriter(detailsBStream);
        detailsBSteamWriter.Write(encrypted);
        
        // Suppression du fichier detailsAFile
        detailsAStreamReader.Dispose();
        detailsAStream.Dispose();
        detailsAFile.Delete();
    }
    
    private void SaveToFile(LocalSessionProfileDetails localSessionProfileDetails, LocalSessionProfile localSessionProfile, string localPath)
    {
        using var zipArchive = ZipFile.Open(localPath, ZipArchiveMode.Create);
        
        var json = JsonHelper.Serialize(localSessionProfileDetails);
        var encrypted = CryptographyUtils.Encrypt(json, _applicationSettingsRepository.EncryptionPassword);
        var detailsAFile = zipArchive.CreateEntry("details_A.json");
        using (var detailsAStream = detailsAFile.Open())
        {
            using (var detailsAStreamWriter = new StreamWriter(detailsAStream))
            {
                detailsAStreamWriter.Write(encrypted);
            }
        }

        json = JsonHelper.Serialize(localSessionProfile);
        var infoFile = zipArchive.CreateEntry("info.json");
        using var entryStream = infoFile.Open();
        using var streamWriter = new StreamWriter(entryStream);
        streamWriter.Write(json);
    }

    private void SaveToFile(CloudSessionProfileDetails cloudSessionProfileDetails, string profileDetailsPassword, string localPath)
    {
        using var zipArchive = ZipFile.Open(localPath, ZipArchiveMode.Create);
        
        var json = JsonHelper.Serialize(cloudSessionProfileDetails);

        var encrypted = CryptographyUtils.Encrypt(json, profileDetailsPassword);

        var detailsAFile = zipArchive.CreateEntry("details_A.json");

        using var entryStream = detailsAFile.Open();
        using var streamWriter = new StreamWriter(entryStream);
        streamWriter.Write(encrypted);
    }

    private CloudSessionProfileDetails BuildCloudSessionProfileDetails(CloudSessionProfileData data, string profileName,
        CloudSessionProfileOptions cloudSessionProfileOptions)
    {
        var cloudSessionProfileDetails = new CloudSessionProfileDetails();
        
        cloudSessionProfileDetails.Name = profileName.Trim();
        cloudSessionProfileDetails.CloudSessionProfileId = data.CloudSessionProfileId;
        cloudSessionProfileDetails.CreationDatetime = data.CreationDateTime;
        cloudSessionProfileDetails.CreatedWithVersion = _connectionService.CurrentEndPoint!.Version;
        
        // Ici, compléter avec les autres options :
        // LoadOnly, RunInventory, RunInventoryAndSynchronization
        // RestrictionIP
        cloudSessionProfileDetails.Options = cloudSessionProfileOptions;

        var allPathItems = _dataSourceRepository.Elements.ToList();
        foreach (var sessionMemberInfo in _sessionMemberRepository.Elements)
        {
            var cloudSessionProfileMember = new CloudSessionProfileMember();

            cloudSessionProfileMember.MachineName = sessionMemberInfo.MachineName;
            cloudSessionProfileMember.Letter = sessionMemberInfo.GetLetter();
            cloudSessionProfileMember.IpAddress = sessionMemberInfo.IpAddress;
            cloudSessionProfileMember.ProfileClientId =
                data.Slots
                    .Single(t => t.ClientId.Equals(sessionMemberInfo.Endpoint.ClientId)).ProfileClientId;
            cloudSessionProfileMember.ProfileClientPassword = $"CSPCP_{Guid.NewGuid()}";
            cloudSessionProfileMember.PathItems = allPathItems
                .Where(pi => pi.BelongsTo(sessionMemberInfo))
                .OrderBy(pivm => pivm.Code)
                .Select(pi => new SessionProfilePathItem(pi))
                .ToList();

            cloudSessionProfileDetails.Members.Add(cloudSessionProfileMember);
        }
        
        var synchronizationRules = _synchronizationRulesConverter.ConvertLooseSynchronizationRules(
            _synchronizationRuleRepository.Elements.ToList());
        cloudSessionProfileDetails.SynchronizationRules.AddAll(synchronizationRules);

        return cloudSessionProfileDetails;
    }
    
    
    private LocalSessionProfileDetails BuildLocalSessionProfileDetails(string profileName, LocalSessionProfileOptions localSessionProfileOptions)
    {
        var localSessionProfileDetails = new LocalSessionProfileDetails();
        
        localSessionProfileDetails.Name = profileName.Trim();
        localSessionProfileDetails.LocalSessionProfileId = $"LSPID_{Guid.NewGuid()}";
        localSessionProfileDetails.CreationDatetime = DateTime.Now;
        localSessionProfileDetails.CreatedWithVersion = _connectionService.CurrentEndPoint!.Version;
        localSessionProfileDetails.Options = localSessionProfileOptions;
        localSessionProfileDetails.PathItems = _dataSourceRepository.SortedCurrentMemberPathItems;
        
        var synchronizationRules = _synchronizationRulesConverter.ConvertLooseSynchronizationRules(
            _synchronizationRuleRepository.Elements.ToList());
        localSessionProfileDetails.SynchronizationRules.AddAll(synchronizationRules);

        return localSessionProfileDetails;
    }
}