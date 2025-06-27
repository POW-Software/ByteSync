using System.Reactive.Linq;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.PushReceivers;

public class PathItemPushReceiver : IPushReceiver
{
    private readonly ISessionService _sessionService;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly IHubPushHandler2 _hubPushHandler2;
    private readonly IDataSourceService _dataSourceService;

    public PathItemPushReceiver(ISessionService sessionService, IDataEncrypter dataEncrypter,
        IHubPushHandler2 hubPushHandler2, IDataSourceService dataSourceService)
    {
        _sessionService = sessionService;
        _dataEncrypter = dataEncrypter;
        _hubPushHandler2 = hubPushHandler2;
        _dataSourceService = dataSourceService;
        
        _hubPushHandler2.PathItemAdded
            .Where(dto => _sessionService.CheckSession(dto.SessionId))
            .Subscribe(dto =>
            {
                var pathItem = _dataEncrypter.DecryptPathItem(dto.EncryptedPathItem);
                _dataSourceService.ApplyAddDataSourceLocally(pathItem);
            });
        
        _hubPushHandler2.PathItemRemoved
            .Where(dto => _sessionService.CheckSession(dto.SessionId))
            .Subscribe(dto =>
            {
                var pathItem = _dataEncrypter.DecryptPathItem(dto.EncryptedPathItem);
                _dataSourceService.ApplyRemoveDataSourceLocally(pathItem);
            });
    }
}