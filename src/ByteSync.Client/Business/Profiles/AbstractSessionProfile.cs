namespace ByteSync.Business.Profiles;

public abstract class AbstractSessionProfile
{
    public string ProfileId { get; set; }
    
    public string Name { get; set; }
    
    public DateTime CreationDatetime { get; set; }
    
    public DateTime? LastRunDatetime { get; set; }
    
    public abstract int MembersCount { get; }
    
    public abstract ProfileTypes ProfileType { get; }
    
    public string CreatedWithVersion { get; set; }
}