namespace ByteSync.Business.Profiles;

public class LocalSessionProfile : AbstractSessionProfile
{
    public LocalSessionProfile()
    {

    }

    public override int MembersCount
    {
        get
        {
            return 1;
        } 
    }
    
    public override ProfileTypes ProfileType => ProfileTypes.Local;
}