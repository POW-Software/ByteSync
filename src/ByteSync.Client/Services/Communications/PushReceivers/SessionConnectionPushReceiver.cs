using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ByteSync.Interfaces.Services.Sessions.Connecting.Joining;
using ByteSync.Interfaces.Services.Sessions.Connecting.Validating;

namespace ByteSync.Services.Communications.PushReceivers;

public class SessionConnectionPushReceiver : IPushReceiver
{
    public SessionConnectionPushReceiver(IHubPushHandler2 hubPushHandler2,
        IYouJoinedSessionService youJoinedSessionService, IYouGaveAWrongPasswordService youGaveAWrongPasswordService,
        ICloudSessionPasswordExchangeKeyAskedService cloudSessionPasswordExchangeKeyAskedService,
        ICloudSessionPasswordExchangeKeyGivenService cloudSessionPasswordExchangeKeyGivenService,
        ICheckCloudSessionPasswordExchangeKeyService checkCloudSessionPasswordExchangeKeyService)
    {
        hubPushHandler2.YouJoinedSession
            .Subscribe(p => youJoinedSessionService.Process(p.Item1, p.Item2));
        
        hubPushHandler2.YouGaveAWrongPassword
            .Subscribe(s => youGaveAWrongPasswordService.Process(s));

        hubPushHandler2.AskCloudSessionPasswordExchangeKey
            .Subscribe(p => cloudSessionPasswordExchangeKeyAskedService.Process(p));
        
        hubPushHandler2.GiveCloudSessionPasswordExchangeKey
            .Subscribe(p => cloudSessionPasswordExchangeKeyGivenService.Process(p));
        
        hubPushHandler2.CheckCloudSessionPasswordExchangeKey
            .Subscribe(p => checkCloudSessionPasswordExchangeKeyService.Process(p));
    }
}