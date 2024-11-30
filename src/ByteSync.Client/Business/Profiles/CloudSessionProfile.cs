namespace ByteSync.Business.Profiles;

public class CloudSessionProfile : AbstractSessionProfile
{
    public CloudSessionProfile()
    {;
        MembersProfileClientIds = new List<string>();
    }
    
    public string ProfileClientId { get; set; }
    
    public List<string> MembersProfileClientIds { get; set; }

    public override int MembersCount
    {
        get
        {
            return MembersProfileClientIds.Count;
        }
    }

    public override ProfileTypes ProfileType => ProfileTypes.Cloud;

    public bool IsManagedByLocalMember
    {
        get
        {
            return MembersProfileClientIds.Count > 0 && MembersProfileClientIds[0].Equals(ProfileClientId);
        }
    }
}