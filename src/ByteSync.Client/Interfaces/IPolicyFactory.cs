using System.Net.Http;
using Azure;
using Azure.Storage.Blobs.Models;
using Polly.Retry;

namespace ByteSync.Interfaces;

public interface IPolicyFactory
{
    AsyncRetryPolicy<Response> BuildFileDownloadPolicy(); 
    
    AsyncRetryPolicy<Response<BlobContentInfo>> BuildFileUploadPolicy();
}