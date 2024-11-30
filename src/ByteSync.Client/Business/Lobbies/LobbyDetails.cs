using System.Collections.ObjectModel;
using System.Threading;
using ByteSync.Business.Profiles;
using ByteSync.ViewModels.Lobbies;

namespace ByteSync.Business.Lobbies;

// ReSharper disable once ClassNeverInstantiated.Global : instancié par LobbyDataHolder / AbstractDataHolder
public class LobbyDetails
{
    public LobbyDetails(string lobbyId)
    {
        LobbyId = lobbyId;
        
        LobbyMembersViewModels = new ObservableCollection<LobbyMemberViewModel>();

        CheckedOtherMembers = new List<CheckedOtherMember>();
        
        TrustCheckWaitHandle = new ManualResetEvent(false);
        AllOtherMembersCheckedWaitHandle = new ManualResetEvent(false);
        LobbyEndedEvent = new ManualResetEvent(false);
        ExpectedMemberWaitHandler = new AutoResetEvent(false);
        SecurityCheckProcessEndedWithSuccess = new ManualResetEvent(false);
    }
    
    public string LobbyId { get; }
    
    public ObservableCollection<LobbyMemberViewModel> LobbyMembersViewModels { get; set; }

    public CloudSessionProfileDetails ProfileDetails { get; set; } = null!;
    
    public CloudSessionProfile Profile { get; set; } = null!;
    
    public string LocalClientInstanceId { get; set; } = null!;
    
    public string LocalProfileClientId { get; set; } = null!;
    
    public List<CheckedOtherMember> CheckedOtherMembers { get; set; }
    
    public bool? IsTrustSuccess { get; set; }
    
    public bool? IsSecurityCheckSuccess { get; set; }
    
    public ManualResetEvent TrustCheckWaitHandle { get; set; }
    
    public ManualResetEvent AllOtherMembersCheckedWaitHandle { get; set; }
    
    public ManualResetEvent LobbyEndedEvent { get; set; }
    
    public ManualResetEvent SecurityCheckProcessEndedWithSuccess { get; set; }
    
    public AutoResetEvent ExpectedMemberWaitHandler { get; set; }

    public LobbyMemberViewModel LocalLobbyMemberViewModel
    {
        get
        {
            return LobbyMembersViewModels
                .Single(m => m.LobbyMember.LobbyMemberInfo != null &&
                                      m.LobbyMember.LobbyMemberInfo.ClientInstanceId.Equals(LocalClientInstanceId));
        }
    }

    public List<LobbyMember> AllLobbyMembers
    {
        get
        {
            return LobbyMembersViewModels
                .Select(m => m.LobbyMember)
                .ToList();
        }
    }
    
    public List<LobbyMember> OtherLobbyMembers
    {
        get
        {
            return AllLobbyMembers
                .Where(m => !m.ProfileClientId.Equals(LocalProfileClientId))
                .ToList();
        }
    }

    public LobbySessionExpectedMember? ExpectedJoiningMember { get; set; }

    public bool HasEnded
    {
        get
        {
            bool isSet = LobbyEndedEvent.WaitOne(0);

            return isSet;
        }
    }
}