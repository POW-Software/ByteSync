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

    public PeerTrustProcessData AddPeerTrustProcessData(PeerTrustProcessData peer)
    {
        var instance = peer.OtherPartyClientInstanceId!;
        if (PeerTrustProcessDatas.TryGetValue(instance, out var existing) &&
            existing.OtherPartyIdentity == peer.OtherPartyIdentity && existing.ExchangeId == peer.ExchangeId)
            return existing;
        PeerTrustProcessDatas[instance] = peer;
        return peer;
    }

    public void SetOtherPartyChecked(ByteSync.Common.Business.Sessions.Cloud.Connections.PublicKeyValidationParameters parameters)
    {
        if (parameters.IssuerClientInstanceId != null &&
            PeerTrustProcessDatas.TryGetValue(parameters.IssuerClientInstanceId, out var peer) &&
            peer.OtherPartyIdentity == parameters.IssuerIdentity && peer.ExchangeId == parameters.ExchangeId &&
            !peer.OtherPartyHasFinished())
            peer.SetOtherPartyChecked(parameters.IsValidated);
    }
}
