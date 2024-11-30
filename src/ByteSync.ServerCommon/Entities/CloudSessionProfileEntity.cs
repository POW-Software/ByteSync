using ByteSync.Common.Business.Profiles;

namespace ByteSync.ServerCommon.Entities;

public class CloudSessionProfileEntity
{
    public CloudSessionProfileEntity()
    {
        CreationDateTime = DateTime.UtcNow;
        LastUse = null;

        Slots = new List<CloudSessionProfileSlot>();
    }
    
    public string CloudSessionProfileId { get; set; }
    
    public List<CloudSessionProfileSlot> Slots { get; set; }
    
    public string ProfileDetailsPassword { get; set; }
    
    public DateTime CreationDateTime { get; set; }
    
    public string? CurrentLobbyId { get; set; }
    
    public DateTime? LastUse { get; set; }
}