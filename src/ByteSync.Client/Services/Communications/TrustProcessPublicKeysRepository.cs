using System.Collections.ObjectModel;
using System.Threading;
using ByteSync.Business.Communications.PublicKeysTrusting;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Controls;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Services.Communications;

namespace ByteSync.Services.Communications;

public class TrustProcessPublicKeysRepository : BaseRepository<TrustProcessPublicKeysData>, ITrustProcessPublicKeysRepository
{
    public TrustProcessPublicKeysRepository(ILogger<TrustProcessPublicKeysRepository> logger)
        : base(logger)
    {

    }

    public async Task Start(string sessionId)
    {
        await ResetDataAsync(sessionId);
    }
    
    public async Task SetExpectedPublicKeyCheckDataCount(string sessionId, List<string> cloudSessionMembersIds)
    {
        await RunAsync(sessionId, data => data.JoinerTrustProcessData.SetExpectedPublicKeyCheckDataCount(cloudSessionMembersIds));
    }
    
    public async Task SetFullyTrusted(string sessionId, PublicKeyCheckData publicKeyCheckData)
    {
        await RunAsync(sessionId, data => data.JoinerTrustProcessData.FullyTrustedPublicKeyCheckDatas.Add(publicKeyCheckData));
    }

    public async Task<bool> IsFullyTrusted(string sessionId, string memberInstanceId)
    {
        return await GetAsync(sessionId,
            data => data.JoinerTrustProcessData
                .FullyTrustedPublicKeyCheckDatas.Any(pk => pk.IssuerClientInstanceId.Equals(memberInstanceId)));
    }

    public async Task<PeerTrustProcessData> ResetPeerTrustProcessData(string sessionId, string otherPartyClientId)
    {
        return await GetAsync(sessionId, data =>
        {
            var peerTrustProcessData = new PeerTrustProcessData(otherPartyClientId);
            
            data.AddPeerTrustProcessData(peerTrustProcessData);

            return peerTrustProcessData;
        });
    }
    
    public async Task ResetJoinerTrustProcessData(string sessionId)
    {
        await RunAsync(sessionId, data => data.JoinerTrustProcessData = new JoinerTrustProcessData());
    }
    
    public async Task<ReadOnlyCollection<PublicKeyCheckData>> GetReceivedPublicKeyCheckData(string sessionId)
    {
        return await GetAsync(sessionId, data => data.JoinerTrustProcessData.GetReceivedPublicKeyCheckData());
    }
    
    public async Task SetOtherPartyChecked(string sessionId, PublicKeyValidationParameters publicKeyValidationParameters)
    {
        await RunAsync(sessionId, data =>
        {
            data.SetOtherPartyChecked(publicKeyValidationParameters.IssuerClientId, publicKeyValidationParameters.IsValidated);
        });
    }
    
    public async Task StoreLocalPublicKeyCheckData(string sessionId, string joinerClientInstanceId, PublicKeyCheckData localPublicKeyCheckData)
    {
        await RunAsync(sessionId, data =>
        {
            data.SessionMemberTrustProcessData.StoreLocalPublicKeyCheckData(joinerClientInstanceId, localPublicKeyCheckData);
        });
    }

    public async Task<PublicKeyCheckData?> GetLocalPublicKeyCheckData(string sessionId, string joinerClientInstanceId)
    {
        return await GetAsync(sessionId, data =>
        {
            return data.SessionMemberTrustProcessData.GetLocalPublicKeyCheckData(joinerClientInstanceId);
        });
    }

    protected override string GetDataId(TrustProcessPublicKeysData data)
    {
        return data.SessionId;
    }

    protected override ManualResetEvent? GetEndEvent(TrustProcessPublicKeysData data)
    {
        return null;
    }
}