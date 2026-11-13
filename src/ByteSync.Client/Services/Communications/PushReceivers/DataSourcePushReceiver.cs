using System.Reactive.Linq;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.PushReceivers;

public class DataSourcePushReceiver : IPushReceiver
{
    private readonly ILocalSourceAuthorizationService _authorization;

    private readonly ISessionService _sessionService;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly IHubPushHandler2 _hubPushHandler2;
    private readonly IDataSourceService _dataSourceService;

    public DataSourcePushReceiver(ISessionService sessionService, IDataEncrypter dataEncrypter,
        IHubPushHandler2 hubPushHandler2, IDataSourceService dataSourceService, ILocalSourceAuthorizationService authorization)
    {
        _authorization = authorization;
        _sessionService = sessionService;
        _dataEncrypter = dataEncrypter;
        _hubPushHandler2 = hubPushHandler2;
        _dataSourceService = dataSourceService;
        
        _hubPushHandler2.DataSourceAdded
            .Where(dto => _sessionService.CheckSession(dto.SessionId))
            .Subscribe(dto =>
            {
                var dataSource = _dataEncrypter.DecryptDataSource(dto.EncryptedDataSource);
                if (dataSource.Id != dto.EncryptedDataSource.Id) throw new UnauthorizedAccessException("Source id mismatch");
                _authorization.CheckNetworkSource(dataSource, dto.ClientInstanceId);
                _dataSourceService.ApplyAddDataSourceLocally(dataSource);
            });
        
        _hubPushHandler2.DataSourceRemoved
            .Where(dto => _sessionService.CheckSession(dto.SessionId))
            .Subscribe(dto =>
            {
                var dataSource = _dataEncrypter.DecryptDataSource(dto.EncryptedDataSource);
                if (dataSource.Id != dto.EncryptedDataSource.Id) throw new UnauthorizedAccessException("Source id mismatch");
                _authorization.CheckNetworkSource(dataSource, dto.ClientInstanceId);
                _dataSourceService.ApplyRemoveDataSourceLocally(dataSource);
            });
    }
}