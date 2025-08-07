using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ITransferLocationService
{
    Task<string> GetUploadFileUrl(string sessionId, Client client, TransferParameters transferParameters);
    
    Task<string> GetDownloadFileUrl(string sessionId, Client client, TransferParameters transferParameters);

    bool IsSharedFileDefinitionAllowed(SessionMemberData? sessionMemberData, SharedFileDefinition? sharedFileDefinition);
}