using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.Connecting;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Controls;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Repositories;

public class CloudSessionConnectionRepository : BaseRepository<CloudSessionConnectionData>, ICloudSessionConnectionRepository
{
    private readonly BehaviorSubject<SessionConnectionStatus> _connectionStatus;
    private readonly BehaviorSubject<CreateSessionError?> _createSessionError;
    private readonly BehaviorSubject<JoinSessionError?> _joinSessionError;

    public CloudSessionConnectionRepository()
    {
        _connectionStatus = new BehaviorSubject<SessionConnectionStatus>(SessionConnectionStatus.NoSession);
        _createSessionError = new BehaviorSubject<CreateSessionError?>(null);
        _joinSessionError = new BehaviorSubject<JoinSessionError?>(null);
        
        CancellationTokenSource = new CancellationTokenSource();
    }

    public byte[]? AesEncryptionKey { get; set; }
    
    protected override string GetDataId(CloudSessionConnectionData data)
    {
        return data.TempSessionId;
    }

    protected override ManualResetEvent? GetEndEvent(CloudSessionConnectionData data)
    {
        return null;
    }

    public async Task SetCloudSessionConnectionData(string sessionId, string sessionPassword,
        RunCloudSessionProfileInfo? lobbySessionDetails)
    {
        await ResetDataAsync(sessionId, newConnectionData =>
        {
            newConnectionData.TempSessionPassword = sessionPassword;
            
            newConnectionData.TempLobbySessionDetails = lobbySessionDetails;
        });
    }

    public Task<string?> GetTempSessionPassword(string sessionId)
    {
        return GetAsync(sessionId, data => data.TempSessionPassword);
    }

    public Task<RunCloudSessionProfileInfo?> GetTempLobbySessionDetails(string sessionId)
    {
        return GetAsync(sessionId, data => data.TempLobbySessionDetails);
    }

    public async Task SetPasswordExchangeKeyReceived(string sessionId)
    {
        await RunAsync(sessionId, data =>
        {
            data.WaitForPasswordExchangeKeyEvent.Set();
        });
    }

    public async Task SetJoinSessionResultReceived(string sessionId)
    {
        await RunAsync(sessionId, data =>
        {
            data.WaitForJoinSessionEvent.Set();
        });
    }

    public async Task<bool> CheckConnectingCloudSession(string? sessionId)
    {
        if (sessionId.IsNullOrEmpty())
        {
            return false;
        }
        
        var data = await GetDataAsync();
        return data != null && sessionId!.Equals(data.TempSessionId, StringComparison.InvariantCultureIgnoreCase);
    }

    public void SetAesEncryptionKey(byte[] aesEncryptionKey)
    {
        lock (SyncRoot)
        {
            AesEncryptionKey = aesEncryptionKey;
        }
    }

    public byte[]? GetAesEncryptionKey()
    {
        lock (SyncRoot)
        {
            return AesEncryptionKey;
        }
    }
    
    public IObservable<SessionConnectionStatus> ConnectionStatusObservable => _connectionStatus.AsObservable();
    
    public IObservable<CreateSessionError?> CreateSessionErrorObservable => _createSessionError.AsObservable();
    
    public IObservable<JoinSessionError?> JoinSessionErrorObservable => _joinSessionError.AsObservable();
    
    public SessionConnectionStatus CurrentConnectionStatus => _connectionStatus.Value;
    
    public CancellationTokenSource CancellationTokenSource { get; set; }
    
    public CancellationToken CancellationToken => CancellationTokenSource.Token;

    public void SetConnectionStatus(SessionConnectionStatus connectionStatus)
    {
        _connectionStatus.OnNext(connectionStatus);
    }
    
    public void SetCreateSessionError(CreateSessionError createSessionError)
    {
        _createSessionError.OnNext(createSessionError);
    }
    
    public void SetJoinSessionError(JoinSessionError joinSessionError)
    {
        CancellationTokenSource.Cancel();
        
        _joinSessionError.OnNext(joinSessionError);
    }

    public void ClearErrors()
    {
        _createSessionError.OnNext(null);
        _joinSessionError.OnNext(null);
    }
}