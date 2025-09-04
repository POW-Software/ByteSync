using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ISharedFilesService
{
    Task AssertFilePartIsUploaded(TransferParameters transferParameters, ICollection<string> recipients);

    Task AssertUploadIsFinished(TransferParameters transferParameters, ICollection<string> recipients);

    Task AssertFilePartIsDownloaded(Client client, TransferParameters transferParameters);
    
    Task ClearSession(string sessionId);
}