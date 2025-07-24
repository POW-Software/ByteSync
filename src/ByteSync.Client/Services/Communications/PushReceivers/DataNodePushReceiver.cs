using System.Reactive.Linq;
using ByteSync.Business.DataNodes;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.PushReceivers;

public class DataNodePushReceiver : IPushReceiver
{
    private readonly ISessionService _sessionService;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly IHubPushHandler2 _hubPushHandler2;
    private readonly IDataNodeService _dataNodeService;

    public DataNodePushReceiver(ISessionService sessionService, IDataEncrypter dataEncrypter,
        IHubPushHandler2 hubPushHandler2, IDataNodeService dataNodeService)
    {
        _sessionService = sessionService;
        _dataEncrypter = dataEncrypter;
        _hubPushHandler2 = hubPushHandler2;
        _dataNodeService = dataNodeService;

        _hubPushHandler2.DataNodeAdded
            .Where(dto => _sessionService.CheckSession(dto.SessionId))
            .Subscribe(dto =>
            {
                var dataNode = _dataEncrypter.DecryptDataNode(dto.EncryptedDataNode);
                dataNode.ClientInstanceId = dto.ClientInstanceId;
                _dataNodeService.ApplyAddDataNodeLocally(dataNode);
            });

        _hubPushHandler2.DataNodeRemoved
            .Where(dto => _sessionService.CheckSession(dto.SessionId))
            .Subscribe(dto =>
            {
                var dataNode = _dataEncrypter.DecryptDataNode(dto.EncryptedDataNode);
                dataNode.ClientInstanceId = dto.ClientInstanceId;
                _dataNodeService.ApplyRemoveDataNodeLocally(dataNode);
            });
    }
}

