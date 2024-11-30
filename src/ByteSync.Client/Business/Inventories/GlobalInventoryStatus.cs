using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions;

namespace ByteSync.Business.Inventories;

public class GlobalInventoryStatus
{
    public GlobalInventoryStatus(ByteSyncEndpoint endpoint, bool isLocal, SessionMemberGeneralStatus newStatus,
        SessionMemberGeneralStatus? previousStatus)
    {
        Endpoint = endpoint;
        IsLocal = isLocal;
        NewStatus = newStatus;
        PreviousStatus = previousStatus;
    }

    public ByteSyncEndpoint Endpoint { get; }
        
    public bool IsLocal { get; }
        
    public SessionMemberGeneralStatus NewStatus { get; }
        
    public SessionMemberGeneralStatus? PreviousStatus { get; }
}