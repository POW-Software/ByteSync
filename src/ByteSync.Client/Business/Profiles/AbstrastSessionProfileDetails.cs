using ByteSync.Business.Actions.Loose;

namespace ByteSync.Business.Profiles;

public abstract class AbstrastSessionProfileDetails
{
    protected AbstrastSessionProfileDetails()
    {
        SynchronizationRules = new List<LooseSynchronizationRule>();
    }
    
    public abstract string ProfileId { get; }
    
    public List<LooseSynchronizationRule> SynchronizationRules { get; set; }
    
    public string Name { get; set; } = null!;
    
    public DateTime CreationDatetime { get; set; }
    
    public string CreatedWithVersion { get; set; } = null!;
}