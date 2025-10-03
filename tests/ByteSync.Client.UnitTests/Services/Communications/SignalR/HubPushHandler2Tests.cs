using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.SignalR;

[TestFixture]
public class HubPushHandler2Tests
{
    /*
    [Test]
    public async Task Test_SetConnection_CreatesObservables()
    {
        // Arrange
        var connectionMock = new Mock<HubConnection>();
        var pingValue = "pingValue";
        var cloudSessionResult = new CloudSessionResult(); // Please initialize this correctly
        var validateJoinCloudSessionParameters = new ValidateJoinCloudSessionParameters(); // Please initialize this correctly
    
        connectionMock
            .Setup(c => c.On(It.IsAny<string>(), It.IsAny<Action<string>>()))
            .Callback<string, Action<string>>((methodName, action) =>
            {
                if (methodName == nameof(IHubByteSyncPush.Ping))
                {
                    action(pingValue);
                }
            });
    
        connectionMock
            .Setup(c => c.On(It.IsAny<string>(), It.IsAny<Action<CloudSessionResult, ValidateJoinCloudSessionParameters>>()))
            .Callback<string, Action<CloudSessionResult, ValidateJoinCloudSessionParameters>>((methodName, action) =>
            {
                if (methodName == nameof(IHubByteSyncPush.YouJoinedSession))
                {
                    action(cloudSessionResult, validateJoinCloudSessionParameters);
                }
            });
    
        var handler = new HubPushHandler2();
    
        // Act
        await handler.SetConnection(connectionMock.Object);
    
        // Assert
        ClassicAssert.IsNotNull(handler.Ping);
        ClassicAssert.IsNotNull(handler.YouJoinedSession);
    
        var ping = await handler.Ping.FirstOrDefaultAsync();
        ClassicAssert.AreEqual(pingValue, ping);
    
        var youJoinedSession = await handler.YouJoinedSession.FirstOrDefaultAsync();
        ClassicAssert.AreEqual(cloudSessionResult, youJoinedSession.Item1);
        ClassicAssert.AreEqual(validateJoinCloudSessionParameters, youJoinedSession.Item2);
    }
    */
}