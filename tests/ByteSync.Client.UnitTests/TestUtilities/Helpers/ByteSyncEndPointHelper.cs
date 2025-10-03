using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Client.UnitTests.TestUtilities.Helpers;

public static class ByteSyncEndPointHelper
{
    public static ByteSyncEndpoint BuildEndPoint(string clientInstanceId = "CID0")
    {
        ByteSyncEndpoint byteSyncEndpoint = new ByteSyncEndpoint();
        byteSyncEndpoint.ClientInstanceId = clientInstanceId;
        
        return byteSyncEndpoint;
    }
}