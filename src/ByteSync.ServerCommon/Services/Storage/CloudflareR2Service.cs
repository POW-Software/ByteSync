using Amazon.S3;
using Amazon.S3.Model;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using ByteSync.ServerCommon.Interfaces.Services.Storage.Factories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services.Storage;

public class CloudflareR2Service : ICloudflareR2Service
{
    private readonly CloudflareR2Settings _cloudflareR2Settings;
    private readonly ILogger<CloudflareR2Service> _logger;
    private readonly ICloudflareR2ClientFactory _clientFactory;

    public CloudflareR2Service(IOptions<CloudflareR2Settings> cloudflareR2Settings,
        ICloudflareR2ClientFactory clientFactory,
        ILogger<CloudflareR2Service> logger)
    {
        _cloudflareR2Settings = cloudflareR2Settings.Value;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<string> GetUploadFileUrl(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var s3Client = _clientFactory.GetClient();
        var key = GetServerFileName(sharedFileDefinition, partNumber);
        
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _cloudflareR2Settings.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(60)
        };

        var preSignedUrl = await s3Client.GetPreSignedURLAsync(request);
        return preSignedUrl;
    }

    public async Task<string> GetDownloadFileUrl(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var s3Client = _clientFactory.GetClient();
        var key = GetServerFileName(sharedFileDefinition, partNumber);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _cloudflareR2Settings.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(20)
        };

        return await s3Client.GetPreSignedURLAsync(request);
    }

    public async Task DeleteObject(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var s3Client = _clientFactory.GetClient();
        string finalFileName = GetServerFileName(sharedFileDefinition, partNumber);

        var request = new DeleteObjectRequest
        {
            BucketName = _cloudflareR2Settings.BucketName,
            Key = finalFileName
        };

        var response = await s3Client.DeleteObjectAsync(request);
        if (!response.HttpStatusCode.Equals(System.Net.HttpStatusCode.OK))
        {
            _logger.LogWarning("Blob {FileName} not found", finalFileName);
        }
    }

    private string GetServerFileName(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var fileName = sharedFileDefinition.GetFileName(partNumber);
        var serverFileName = sharedFileDefinition.SessionId + "_" + sharedFileDefinition.ClientInstanceId + "_" + fileName;
        
        return serverFileName;
    }

    public async Task<IReadOnlyCollection<KeyValuePair<string, DateTimeOffset?>>> GetAllObjects(CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();
        var request = new ListObjectsV2Request
        {
            BucketName = _cloudflareR2Settings.BucketName
        };

        var response = await client.ListObjectsV2Async(request, cancellationToken);
        return response.S3Objects?
            .Select(o => new KeyValuePair<string, DateTimeOffset?>(o.Key, o.LastModified))
            .ToList() ?? [];
    }

    public async Task DeleteObjectByKey(string key, CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = _cloudflareR2Settings.BucketName,
            Key = key
        };

        await client.DeleteObjectAsync(deleteRequest, cancellationToken);
    }
} 