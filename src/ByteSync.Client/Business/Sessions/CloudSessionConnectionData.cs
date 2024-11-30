using System.Threading;
using ByteSync.Business.Sessions.RunSessionInfos;

namespace ByteSync.Business.Sessions;

public class CloudSessionConnectionData
{
    public CloudSessionConnectionData(string sessionId)
    {
        TempSessionId = sessionId;
        
        WaitTimeSpan = TimeSpan.FromSeconds(60);

        // PublicKeyCheckDataHolder = new PublicKeyCheckDataHolder();


        // AuthCheckedPublicKeyInfos = new List<string>();

        WaitForPasswordExchangeKeyEvent = new ManualResetEvent(false);
        WaitForJoinSessionEvent = new ManualResetEvent(false);
        // WaitForDigitalSignaturesCheckedEvent = new ManualResetEvent(false);
        
        // LocalTrustValidatedCheckEvent = new ManualResetEvent(false);
        // OtherPartyTrustCheckEvent = new ManualResetEvent(false);
        // LocalTrustCanceledCheckEvent = new ManualResetEvent(false);
    }
    
    public string TempSessionId { get; }

    public string? TempSessionPassword { get; set; }
    
    public RunCloudSessionProfileInfo? TempLobbySessionDetails { get; set; }
    

    
    public ManualResetEvent WaitForPasswordExchangeKeyEvent { get; }

    public ManualResetEvent WaitForJoinSessionEvent { get; }
    


    // public ManualResetEvent LocalTrustValidatedCheckEvent { get; }
    //
    // public ManualResetEvent OtherPartyTrustCheckEvent { get; }
    //
    // public ManualResetEvent LocalTrustCanceledCheckEvent { get; }

    public TimeSpan WaitTimeSpan { get; }

    // private PublicKeyCheckDataHolder PublicKeyCheckDataHolder { get; set; }
    

    // public bool HasOtherPartyValidatedPublicKey { get; set; }
    //
    // public bool HasLocalValidatedPublicKey { get; set; }    
    //
    // public bool HasLocalCanceledPublicKey { get; set; }
    


    // public bool IsCreatingOrJoiningSession
    // {
    //     get
    //     {
    //         return IsCreatingSession || IsJoiningSession;
    //     }
    // }
    //
    // public bool IsCreatingSession { get; set; }
    //     
    // public bool IsJoiningSession { get; set; }
    
    // public ManualResetEvent SessionEndEvent { get; set; }
}