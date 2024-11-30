namespace ByteSync.ServerCommon.Business.Settings;

public class RedisSettings
{
    public string ConnectionString { get; set; } = "";
    
    public string Prefix { get; set; } = "";
}