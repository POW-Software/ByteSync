﻿using System.Net.Http;
using Azure;
using Azure.Storage.Blobs.Models;
using Polly.Retry;
using RestSharp;

namespace ByteSync.Interfaces;

public interface IPolicyFactory
{
    AsyncRetryPolicy BuildHubPolicy();
    
    AsyncRetryPolicy<RestResponse> BuildRestPolicy();
    
    AsyncRetryPolicy<HttpResponseMessage> BuildHttpPolicy(int? maxAttempts = null);
    
    AsyncRetryPolicy<Response> BuildFileDownloadPolicy(); 
    
    AsyncRetryPolicy<Response<BlobContentInfo>> BuildFileUploadPolicy();
}