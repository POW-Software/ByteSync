using ByteSync.Business.Communications.PublicKeysTrusting;

namespace ByteSync.Business;

public class TrustDataParameters
{
    public TrustDataParameters()
    {
        SessionId = "DBG_Id";
        TrustDataParametersId = "DBG_ID";
        // PeerTrustProcessData = new PeerTrustProcessData();
    }
    
    public TrustDataParameters(int currentClientIndex, int clientsCount, bool isJoinerSide, string sessionId,
        PeerTrustProcessData peerTrustProcessData)
    {
        TrustDataParametersId = $"TDPID_{Guid.NewGuid()}";
        CurrentClientIndex = currentClientIndex;
        ClientsCount = clientsCount;
        IsJoinerSide = isJoinerSide;
        SessionId = sessionId;
        PeerTrustProcessData = peerTrustProcessData;
    }

    public string TrustDataParametersId { get; }

    public int ClientsCount { get; set; }
    
    public int CurrentClientIndex { get; set; }

    public bool IsJoinerSide { get; set; }
    
    public string SessionId { get; set; }
    
    public PeerTrustProcessData PeerTrustProcessData { get; } = null!;
}