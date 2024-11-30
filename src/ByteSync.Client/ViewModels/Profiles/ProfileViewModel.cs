using ByteSync.Business.Profiles;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Profiles;

public class ProfileViewModel
{
    public ProfileViewModel(AbstractSessionProfile sessionProfile)
    {
        SessionProfile = sessionProfile;

        Name = SessionProfile.Name;
        Type = SessionProfile.ProfileType;
        Members = SessionProfile.MembersCount;

        Creation = SessionProfile.CreationDatetime;
        LastRun = SessionProfile.LastRunDatetime;

        IsLobbyManagedByMe = sessionProfile is LocalSessionProfile ||
                             (sessionProfile is CloudSessionProfile cloudSessionProfile && cloudSessionProfile.IsManagedByLocalMember);
    }
    
    public AbstractSessionProfile SessionProfile { get; }

    [Reactive]
    public string Name { get; set; }
    
    [Reactive]
    public ProfileTypes Type { get; set; }
    
    [Reactive]
    public int Members { get; set; }
    
    [Reactive]
    public DateTime Creation { get; set; }
    
    [Reactive]
    public DateTime? LastRun { get; set; }
    
    [Reactive] 
    public bool IsLobbyManagedByMe { get; set; }
}