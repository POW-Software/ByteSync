using System.Threading.Tasks;
using ByteSync.Business.Communications;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Interfaces;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IDigitalSignaturesRepository : IRepository<DigitalSignaturesData>
{
    Task Start(string sessionId);
    
    Task SetExpectedDigitalSignaturesToCheck(string dataId, List<DigitalSignatureCheckInfo> signatureCheckInfos);

    Task SetDigitalSignatureChecked(string dataId, string clientInstanceId);

    Task<bool> IsAuthChecked(string dataId, SessionMemberInfoDTO sessionMemberInfo);
}