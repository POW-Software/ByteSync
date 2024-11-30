using System.Threading;
using System.Threading.Tasks;

namespace ByteSync.Business.Communications.PublicKeysTrusting;

public class PeerTrustProcessData
{
    private bool _hasOtherPartyValidatedPublicKey;
    private bool _hasLocalValidatedPublicKey;
    private bool _hasLocalCancelledPublicKey;

    public PeerTrustProcessData(string otherPartyClientId)
    {
        OtherPartyClientId = otherPartyClientId;
        
        LocalTrustValidatedCheckEvent = new ManualResetEvent(false);
        OtherPartyTrustCheckEvent = new ManualResetEvent(false);
        LocalTrustCanceledCheckEvent = new ManualResetEvent(false);

        SyncRoot = new object();
    }
    
    public string OtherPartyClientId { get; }

    public bool HasOtherPartyValidatedPublicKey
    {
        get
        {
            lock (SyncRoot)
            {
                return _hasOtherPartyValidatedPublicKey;
            }
        }
        private set
        {
            lock (SyncRoot)
            {
                _hasOtherPartyValidatedPublicKey = value;
            }
        }
    }

    public bool HasLocalValidatedPublicKey
    {
        get
        {
            lock (SyncRoot)
            {
                return _hasLocalValidatedPublicKey;
            }
        }
        private set
        {
            lock (SyncRoot)
            {
                _hasLocalValidatedPublicKey = value;
            }
        }
    }

    public bool HasLocalCancelledPublicKey
    {
        get
        {
            lock (SyncRoot)
            {
                return _hasLocalCancelledPublicKey;
            }
        }
        private set
        {
            lock (SyncRoot)
            {
                _hasLocalCancelledPublicKey = value;
            }
        }
    }

    public ManualResetEvent LocalTrustValidatedCheckEvent { get; }
    
    public ManualResetEvent OtherPartyTrustCheckEvent { get; }
    
    public ManualResetEvent LocalTrustCanceledCheckEvent { get; }
    
    private object SyncRoot { get; }

    public void SetMyPartyChecked(bool isValidated)
    {
        lock (SyncRoot)
        {
            HasLocalValidatedPublicKey = isValidated;
            LocalTrustValidatedCheckEvent.Set();
        }
    }

    public void SetMyPartyCancelled()
    {
        lock (SyncRoot)
        {
            HasLocalCancelledPublicKey = true;
            LocalTrustCanceledCheckEvent.Set();
        }
    }

    public void SetOtherPartyChecked(bool isValidated)
    {
        lock (SyncRoot)
        {
            HasOtherPartyValidatedPublicKey = isValidated;
            OtherPartyTrustCheckEvent.Set();
        }
    }

    public async Task<bool> WaitForPeerTrustProcessFinished()
    {
        return await Task.Run(() =>
        {
            ManualResetEvent group = new ManualResetEvent(false);

            Task.Run(() =>
            {
                WaitHandle.WaitAll(new WaitHandle[]
                {
                    LocalTrustValidatedCheckEvent, 
                    OtherPartyTrustCheckEvent
                });

                group.Set();
            });
            
            WaitHandle.WaitAny(new WaitHandle[] { group, LocalTrustCanceledCheckEvent });

            return IsPeerTrustSuccess;
        });
    }

    public bool IsPeerTrustSuccess
    {
        get {
            lock (SyncRoot)
            {
                bool isPeerTrustSuccess = HasOtherPartyValidatedPublicKey && HasLocalValidatedPublicKey && !HasLocalCancelledPublicKey;

                return isPeerTrustSuccess;
            }
        }
    }

    public bool OtherPartyHasFinished()
    {
        return OtherPartyTrustCheckEvent.WaitOne(0);
    }
}