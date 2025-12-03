using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ByteSync.Business.Communications.PublicKeysTrusting;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Interfaces;

namespace ByteSync.Interfaces.Controls.Communications;

public interface ITrustProcessPublicKeysRepository : IRepository<TrustProcessPublicKeysData>
{
    public Task Start(string sessionId);
    
    public Task SetExpectedPublicKeyCheckDataCount(string sessionId, List<string> cloudSessionMembersIds);

    Task SetFullyTrusted(string sessionId, PublicKeyCheckData publicKeyCheckData);

    Task<bool> IsFullyTrusted(string sessionId, string memberInstanceId);

    Task<PeerTrustProcessData> ResetPeerTrustProcessData(string sessionId, string otherPartyClientId);
    
    Task ResetJoinerTrustProcessData(string sessionId);
    
    Task<ReadOnlyCollection<PublicKeyCheckData>> GetReceivedPublicKeyCheckData(string sessionId);
    
    Task StoreLocalPublicKeyCheckData(string sessionId, string joinerClientInstanceId, PublicKeyCheckData localPublicKeyCheckData);

    Task<PublicKeyCheckData?> GetLocalPublicKeyCheckData(string sessionId, string joinerClientInstanceId);
    
    Task SetOtherPartyChecked(string sessionId, PublicKeyValidationParameters publicKeyValidationParameters);
    
    Task SetProtocolVersionIncompatible(string sessionId, string memberClientInstanceId);
    
    Task<bool> IsProtocolVersionIncompatible(string sessionId);
}