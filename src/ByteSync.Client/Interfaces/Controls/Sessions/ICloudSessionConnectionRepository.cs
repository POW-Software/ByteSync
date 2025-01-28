using System.Threading.Tasks;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Interfaces;

namespace ByteSync.Interfaces.Controls.Sessions
{
    public interface ICloudSessionConnectionRepository : IRepository<CloudSessionConnectionData>
    {
        void SetAesEncryptionKey(byte[] aesEncryptionKey);
        
        byte[]? GetAesEncryptionKey();

        Task SetCloudSessionConnectionData(string sessionId, string sessionPassword, RunCloudSessionProfileInfo? lobbySessionDetails);

        Task<string?> GetTempSessionPassword(string sessionId);
        
        public Task<RunCloudSessionProfileInfo?> GetTempLobbySessionDetails(string sessionId);

        Task SetPasswordExchangeKeyReceived(string sessionId);

        Task SetJoinSessionResultReceived(string sessionId);
        
        Task<bool> CheckConnectingCloudSession(string? sessionId);
        
        // StatusObservable ici : IsCreatingSession, IsJoiningSession, IsConnectedSession
        // Déjà implémenté dans CloudSessionConnectionService
        
        public IObservable<ConnectionStatuses> ConnectionStatusObservable { get; }
    
        public ConnectionStatuses CurrentConnectionStatus { get; }
    
        void SetConnectionStatus(ConnectionStatuses connectionStatus);
    }
}