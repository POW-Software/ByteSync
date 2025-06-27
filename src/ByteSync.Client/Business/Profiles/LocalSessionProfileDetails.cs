using ByteSync.Business.DataSources;

namespace ByteSync.Business.Profiles;

public class LocalSessionProfileDetails : AbstrastSessionProfileDetails
{
    public LocalSessionProfileDetails()
    {
        Options = new LocalSessionProfileOptions();
        
        PathItems = new List<DataSource>();
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
    
    public IList<DataSource> PathItems { get; set; }
}