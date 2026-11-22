namespace ByteSync.Common.Business.Profiles;

public class CloudSessionProfileSlot
{
    public string ProfileClientId { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string? Identity { get; set; }
    public string? OriginalClientInstanceId { get; set; }
}
