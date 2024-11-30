using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Controls;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Sessions;

namespace ByteSync.Services.Sessions;

public class CloudSessionConnectionRepository : BaseRepository<CloudSessionConnectionData>, ICloudSessionConnectionRepository
{
    public CloudSessionConnectionRepository()
    {

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
}