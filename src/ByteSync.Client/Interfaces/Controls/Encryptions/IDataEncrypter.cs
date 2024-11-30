using ByteSync.Business.PathItems;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;

namespace ByteSync.Interfaces.Controls.Encryptions;

public interface IDataEncrypter
{
    public EncryptedSessionSettings EncryptSessionSettings(SessionSettings sessionSettings);
    
    public SessionSettings DecryptSessionSettings(EncryptedSessionSettings encryptedSessionSettings);
    
    public EncryptedPathItem EncryptPathItem(PathItem pathItem);
    
    public PathItem DecryptPathItem(EncryptedPathItem encryptedPathItem);
    
    public EncryptedSessionMemberPrivateData EncryptSessionMemberPrivateData(SessionMemberPrivateData sessionMemberPrivateData);
    
    public SessionMemberPrivateData DecryptSessionMemberPrivateData(EncryptedSessionMemberPrivateData encryptedSessionMemberPrivateData);
}