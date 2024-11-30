using ByteSync.Business.Lobbies;
using ByteSync.Business.Profiles;
using ByteSync.Common.Helpers;

namespace ByteSync.Services.Lobbies;

public class LobbyCheckKeyComputer
{
    public LobbyCheckKeyComputer(string lobbyId, string senderProfileClientId, string senderMachineIdentifier)
    {
        LobbyId = lobbyId;
        SenderProfileClientId = senderProfileClientId;
        SenderMachineIdentifier = senderMachineIdentifier;
    }

    public LobbyCheckKeyComputer(string lobbyId, LobbyMember senderProfileClientId)
    {
        LobbyId = lobbyId;
        SenderProfileClientId = senderProfileClientId.ProfileClientId;
        SenderMachineIdentifier = senderProfileClientId.LobbyMemberInfo!.PublicKeyInfo.ClientId;
    }

    public string LobbyId { get; }
    
    public string SenderProfileClientId { get; }
    
    private string SenderMachineIdentifier { get; }
    
    public string ComputeKey(CloudSessionProfileMember cloudSessionProfileMember)
    {
        var checkKey = $"{LobbyId}_{SenderProfileClientId}_{SenderMachineIdentifier}_" +
                       $"{cloudSessionProfileMember.ProfileClientId}_{cloudSessionProfileMember.ProfileClientPassword}";
        
        
        
        var sha256 = CryptographyUtils.ComputeSHA256FromText(checkKey);

        return sha256;
    }
}