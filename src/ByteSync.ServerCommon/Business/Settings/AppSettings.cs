namespace ByteSync.ServerCommon.Business.Settings;

public class AppSettings
{
    /// <summary>
    /// JwtSecret
    /// </summary>
    public string Secret { get; set; } = "";
    
    /// <summary>
    /// Jwt tokens duration in seconds
    /// </summary>
    public int JwtDurationInSeconds { get; set; } = 3600;

    public int SaveStorageDelayInSeconds { get; set; } = 3600;
    
    public bool SkipClientsVersionCheck { get; set; } = false;
}