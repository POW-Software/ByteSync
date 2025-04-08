using System.Threading;
using ByteSync.Business.Communications;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Controls;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications;

public class DigitalSignaturesRepository : BaseRepository<DigitalSignaturesData>, IDigitalSignaturesRepository
{
    public DigitalSignaturesRepository(ILogger<DigitalSignaturesRepository> logger) 
        : base(logger)
    {
    }
    
    public async Task Start(string sessionId)
    {
        await ResetDataAsync(sessionId);
    }

    public async Task SetExpectedDigitalSignaturesToCheck(string dataId, List<DigitalSignatureCheckInfo> signatureCheckInfos)
    {
        await RunAsync(dataId, data =>
        {
            data.DigitalSignaturesExpectedClients = signatureCheckInfos.Select(ds => ds.Recipient).ToList().ToHashSet();
            
            data.ExpectedDigitalSignaturesCheckedEvent.Reset();
        });
    }

    public async Task SetDigitalSignatureChecked(string dataId, string clientInstanceId)
    {
        await RunAsync(dataId, data =>
        {
            data.DigitalSignatureCheckedClients.Add(clientInstanceId);
            
            if (data.DigitalSignaturesExpectedClients != null)
            {
                if (data.DigitalSignaturesExpectedClients.ContainsAll(data.DigitalSignatureCheckedClients))
                    // if (data.AuthCheckedPublicKeyInfos.HaveSameElements(data.ExpectedDigitalSignaturesChecks.Select(ds => ds.Recipient).ToList()))
                {
                    data.ExpectedDigitalSignaturesCheckedEvent.Set();
                    
                    // On peut désormais resetter :
                    data.DigitalSignaturesExpectedClients = null;
                    // data.ReceivedDigitalSignaturesChecks = new List<DigitalSignatureCheckInfo>();
                }
            }
        });
    }

    public async Task<bool> IsAuthChecked(string dataId, SessionMemberInfoDTO sessionMemberInfo)
    {
        return await GetAsync(dataId, data =>
        {
            return data.DigitalSignatureCheckedClients.Contains(sessionMemberInfo.ClientInstanceId);
        });
    }

    protected override string GetDataId(DigitalSignaturesData data)
    {
        return data.DataId;
    }

    protected override ManualResetEvent? GetEndEvent(DigitalSignaturesData data)
    {
        return null;
    }
}