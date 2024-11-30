using ByteSync.Business.PathItems;

namespace ByteSync.Business.Profiles;

public class LocalSessionProfileDetails : AbstrastSessionProfileDetails
{
    public LocalSessionProfileDetails()
    {
        Options = new LocalSessionProfileOptions();
        
        PathItems = new List<PathItem>();
    }
    
    public override string ProfileId
    {
        get
        {
            return LocalSessionProfileId;
        }
    }

    public string LocalSessionProfileId { get; set; } = null!;

    public LocalSessionProfileOptions Options { get; set; }
    
    public List<PathItem> PathItems { get; set; }
}