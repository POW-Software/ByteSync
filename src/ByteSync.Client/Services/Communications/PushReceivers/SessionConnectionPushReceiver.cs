using ByteSync.Commands.Sessions.Connecting;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using MediatR;

namespace ByteSync.Services.Communications.PushReceivers;

public class SessionConnectionPushReceiver : IPushReceiver
{
    private readonly IHubPushHandler2 _hubPushHandler2;

    public SessionConnectionPushReceiver(IHubPushHandler2 hubPushHandler2, IMediator mediator)
    {
        _hubPushHandler2 = hubPushHandler2;

        _hubPushHandler2.YouJoinedSession
            .Subscribe(p => mediator.Send(new OnYouJoinedSessionRequest(p.Item1, p.Item2)));
        
        _hubPushHandler2.YouGaveAWrongPassword
            .Subscribe(s => mediator.Send(new OnYouGaveAWrongPasswordRequest(s)));

        _hubPushHandler2.AskCloudSessionPasswordExchangeKey
            .Subscribe(p => mediator.Send(new OnCloudSessionPasswordExchangeKeyAskedRequest(p.SessionId, p.PublicKeyInfo, p.RequesterInstanceId)));
        
        _hubPushHandler2.GiveCloudSessionPasswordExchangeKey
            .Subscribe(p => mediator.Send(new OnCloudSessionPasswordExchangeKeyGivenRequest(p.SessionId, p.JoinerInstanceId, 
                p.ValidatorInstanceId, p.PublicKeyInfo)));
        
        _hubPushHandler2.CheckCloudSessionPasswordExchangeKey
            .Subscribe(p => OnCheckCloudSessionPasswordExchangeKey);
        
        // _hubPushHandler2.HubPushHandler2.OnReconnected
        //     .Subscribe(OnReconnected);
    }
}