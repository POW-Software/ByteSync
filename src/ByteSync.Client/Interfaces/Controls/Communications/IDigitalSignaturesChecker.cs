using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IDigitalSignaturesChecker
{
    Task<bool> CheckExistingMembersDigitalSignatures(string dataId, ICollection<string> clientInstanceIds);

    Task CheckDigitalSignature(DigitalSignatureCheckInfo digitalSignatureCheckInfo);
}