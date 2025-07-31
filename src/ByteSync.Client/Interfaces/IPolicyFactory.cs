using Azure;
using ByteSync.Common.Business.Communications.Transfers;
using Polly.Retry;

namespace ByteSync.Interfaces;

public interface IPolicyFactory
{
    AsyncRetryPolicy<Response> BuildFileDownloadPolicy(); 
    
    AsyncRetryPolicy<UploadFileResponse> BuildFileUploadPolicy();
}