using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.ServerCommon.Business.Settings;

public class AppSettings
{
    public string Secret { get; set; } = "";
    
    public int JwtDurationInSeconds { get; set; } = 3600;
    
    public bool SkipClientsVersionCheck { get; set; } = false;
    
    public bool RetainFilesAfterTransfer { get; set; } = false;

    public string UpdatesDefinitionUrl { get; set; } = "";

    public string AnnouncementsUrl { get; set; } = "";
    
    public StorageProvider DefaultStorageProvider { get; set; } = StorageProvider.AzureBlobStorage;
}