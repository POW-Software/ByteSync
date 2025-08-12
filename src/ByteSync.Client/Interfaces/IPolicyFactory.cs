using ByteSync.Common.Business.Communications.Transfers;
using Polly.Retry;

namespace ByteSync.Interfaces;

public interface IPolicyFactory
{
    AsyncRetryPolicy<DownloadFileResponse> BuildFileDownloadPolicy(); 
    
    AsyncRetryPolicy<UploadFileResponse> BuildFileUploadPolicy();
}