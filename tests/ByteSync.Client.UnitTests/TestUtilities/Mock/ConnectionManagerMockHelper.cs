namespace ByteSync.Client.UnitTests.TestUtilities.Mock;

public static class ConnectionManagerMockHelper
{
    /*
    public static void SetupGetCurrentEndpoint(this Mock<IConnectionManager> connectionManagerMock,
        string clientInstanceId = "CID0")
    {
        connectionManagerMock.Setup(cm => cm.GetCurrentEndpoint())
            .Returns(new ByteSyncEndpoint {ClientInstanceId = clientInstanceId});
    
        connectionManagerMock.SetupGet(cm => cm.ClientInstanceId)
            .Returns(clientInstanceId);
    }
    
    public static HubPushHandler2 SetupPushHandler(this Mock<IConnectionManager> connectionManagerMock)
    {
        var hubPushHandler2 = new HubPushHandler2();
    
        connectionManagerMock.SetupGet(cm => cm.HubPushHandler2)
            .Returns(hubPushHandler2)
            .Verifiable();
    
        return hubPushHandler2;
    }
    */
}