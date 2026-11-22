using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.ServerCommon.Security;

public interface IFileTransferRequest : ISessionRequest
{
    TransferParameters TransferParameters { get; }
}
