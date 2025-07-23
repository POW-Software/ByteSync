using System.Threading.Tasks;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface IFileTransferApiClient
{   
    Task<string> GetUploadFileUrl(TransferParameters transferParameters);
    
    Task<FileSourceInfo> GetDownloadFileMode(TransferParameters transferParameters);
    
    Task AssertFilePartIsUploaded(TransferParameters transferParameters);
    
    Task AssertUploadIsFinished(TransferParameters transferParameters);
    
    Task AssertFilePartIsDownloaded(TransferParameters transferParameters);
    
    Task AssertDownloadIsFinished(TransferParameters transferParameters);
}