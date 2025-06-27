using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Versions;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

namespace ByteSync.Business.Events;

public class CloudSessionEventArgs : EventArgs
{
    public CloudSessionEventArgs(CloudSession cloudSession)
    {
        CloudSession = cloudSession;
    }

    public CloudSession CloudSession { get; private set; }
}
    
public class CloudSessionResultEventArgs : EventArgs
{
    public CloudSessionResultEventArgs(CloudSessionResult cloudSessionResult)
    {
        CloudSessionResult = cloudSessionResult;
    }

    public CloudSessionResult CloudSessionResult { get; private set; }
}

// public class DataSourceEventArgs : EventArgs
// {
//     public DataSourceEventArgs(SessionMemberInfo sessionMemberInfo, DataSource dataSource)
//     {
//         SessionMemberInfo = sessionMemberInfo;
//         DataSource = dataSource;
//     }
//
//     public SessionMemberInfo SessionMemberInfo { get; private set; }
//     
//     public DataSource DataSource { get; private set; }
// }
    
public class InventoryReadyEventArgs : EventArgs
{
    public InventoryReadyEventArgs(LocalInventoryModes localInventoryMode)
    {
        LocalInventoryMode = localInventoryMode;
    }

    public LocalInventoryModes LocalInventoryMode { get; }
}
    
// public class InventoryStatusChangedEventArgs : EventArgs
// {
//     public InventoryStatusChangedEventArgs(ByteSyncEndpoint endpoint, bool isLocal, SessionMemberGeneralStatus newStatus,
//         SessionMemberGeneralStatus? previousStatus)
//     {
//         Endpoint = endpoint;
//         IsLocal = isLocal;
//         NewStatus = newStatus;
//         PreviousStatus = previousStatus;
//     }
//
//     public ByteSyncEndpoint Endpoint { get; }
//     
//     public bool IsLocal { get; }
//     
//     public SessionMemberGeneralStatus NewStatus { get; }
//     
//     public SessionMemberGeneralStatus? PreviousStatus { get; }
// }

public class SoftwareVersionEventArgs : EventArgs
{
    public SoftwareVersionEventArgs(List<SoftwareVersion> softwareVersions)
    {
        SoftwareVersions = softwareVersions;
    }

    public List<SoftwareVersion> SoftwareVersions { get; }
}
    
public class ManualActionCreationRequestedArgs : EventArgs
{
    public ManualActionCreationRequestedArgs(ComparisonResultViewModel requester, List<ComparisonItemViewModel> comparisonItemViewModels)
    {
        Requester = requester;
        ComparisonItemViewModels = comparisonItemViewModels;
    }

    public ComparisonResultViewModel Requester { get; set; }

    public List<ComparisonItemViewModel> ComparisonItemViewModels { get; }
}

public class TrustKeyDataRequestedArgs : EventArgs
{
    public TrustKeyDataRequestedArgs(PublicKeyCheckData publicKeyCheckData, TrustDataParameters trustDataParameters)
    {
        PublicKeyCheckData = publicKeyCheckData;
        TrustDataParameters = trustDataParameters;
    }

    public PublicKeyCheckData PublicKeyCheckData { get; set; }

    public TrustDataParameters TrustDataParameters { get; }
}
    
public class GenericEventArgs<T> : EventArgs
{
    public GenericEventArgs(T value)
    {
        Value = value;
    }

    public T Value { get; }
}

// class FinishedEventArgs : EventArgs
// {
//     public FinishedEventArgs(Exception exception = null)
//     {
//         Exception = exception;
//     }
//
//     public Exception Exception { get; private set; }
// }