namespace ByteSync.ServerCommon.Business.Settings;

public class AppSettings
{
    public string Secret { get; set; } = "";
    
    public int JwtDurationInSeconds { get; set; } = 3600;
    
    public bool SkipClientsVersionCheck { get; set; } = false;

    public string UpdatesDefinitionUrl { get; set; } = "";

    public string MessagesDefinitionsUrl { get; set; } = "";
}