using System.Reactive.Linq;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Services.Communications.PushReceivers;

public class PathItemPushReceiver : IPushReceiver
{
    private readonly ISessionService _sessionService;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly IHubPushHandler2 _hubPushHandler2;
    private readonly IPathItemRepository _pathItemRepository;

    public PathItemPushReceiver(ISessionService sessionService, IDataEncrypter dataEncrypter,
        IHubPushHandler2 hubPushHandler2, IPathItemRepository pathItemRepository)
    {
        _sessionService = sessionService;
        _dataEncrypter = dataEncrypter;
        _hubPushHandler2 = hubPushHandler2;
        _pathItemRepository = pathItemRepository;
        
        _hubPushHandler2.PathItemAdded
            .Where(dto => _sessionService.CheckSession(dto.SessionId))
            .Subscribe(dto =>
            {
                var pathItem = _dataEncrypter.DecryptPathItem(dto.EncryptedPathItem);
                _pathItemRepository.AddOrUpdate(pathItem);
            });
        
        _hubPushHandler2.PathItemRemoved
            .Where(dto => _sessionService.CheckSession(dto.SessionId))
            .Subscribe(dto =>
            {
                var pathItem = _dataEncrypter.DecryptPathItem(dto.EncryptedPathItem);
                _pathItemRepository.Remove(pathItem);
            });
    }
}