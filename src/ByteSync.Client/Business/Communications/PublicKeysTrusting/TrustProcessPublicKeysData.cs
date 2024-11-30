using Serilog;

namespace ByteSync.Business.Communications.PublicKeysTrusting;

public class TrustProcessPublicKeysData
{
    public TrustProcessPublicKeysData(string sessionId)
    {
        SessionId = sessionId;

        JoinerTrustProcessData = new JoinerTrustProcessData();

        PeerTrustProcessDatas = new Dictionary<string, PeerTrustProcessData>();

        SessionMemberTrustProcessData = new SessionMemberTrustProcessData();
    }
    
    public string SessionId { get; }
    
    public JoinerTrustProcessData JoinerTrustProcessData { get; set; }
    
    private Dictionary<string, PeerTrustProcessData> PeerTrustProcessDatas { get; set; }
    
    public SessionMemberTrustProcessData SessionMemberTrustProcessData { get; }

    public void AddPeerTrustProcessData(PeerTrustProcessData peerTrustProcessData)
    {
        if (PeerTrustProcessDatas.ContainsKey(peerTrustProcessData.OtherPartyClientId))
        {
            PeerTrustProcessDatas.Remove(peerTrustProcessData.OtherPartyClientId);
        }
        
        PeerTrustProcessDatas.Add(peerTrustProcessData.OtherPartyClientId, peerTrustProcessData);
    }

    public void SetOtherPartyChecked(string issuerClientId, bool isValidated)
    {
        PeerTrustProcessData? peerTrustProcessData;
        if (PeerTrustProcessDatas.TryGetValue(issuerClientId, out peerTrustProcessData))
        {
            peerTrustProcessData.SetOtherPartyChecked(isValidated);
        }
        else
        {
            Log.Warning("Can not find PeerTrustProcessData with ClientId {issuerClientId}", issuerClientId);
        }
    }
}