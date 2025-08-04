using Amazon.S3;
using Amazon.S3.Model;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services;

public class CloudflareR2UrlService : ICloudflareR2UrlService
{
    private readonly CloudflareR2Settings _cloudflareR2Settings;
    private readonly ILogger<CloudflareR2UrlService> _logger;
    private AmazonS3Client? _s3Client;

    public CloudflareR2UrlService(IOptions<CloudflareR2Settings> cloudflareR2Settings, ILogger<CloudflareR2UrlService> logger)
    {
        _cloudflareR2Settings = cloudflareR2Settings.Value;
        _logger = logger;
    }

    public async Task<string> GetUploadFileUrl(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var s3Client = GetS3Client();
        var key = GetServerFileName(sharedFileDefinition, partNumber);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _cloudflareR2Settings.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(60)
        };

        return await Task.FromResult(s3Client.GetPreSignedURL(request));
    }

    public async Task<string> GetDownloadFileUrl(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var s3Client = GetS3Client();
        var key = GetServerFileName(sharedFileDefinition, partNumber);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _cloudflareR2Settings.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(20)
        };

        return await Task.FromResult(s3Client.GetPreSignedURL(request));
    }

    public async Task DeleteObject(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var s3Client = GetS3Client();
        var key = GetServerFileName(sharedFileDefinition, partNumber);

        _logger.LogInformation("Deleting object {Key} from Cloudflare R2", key);

        var request = new DeleteObjectRequest
        {
            BucketName = _cloudflareR2Settings.BucketName,
            Key = key
        };

        var response = await s3Client.DeleteObjectAsync(request);
        if (response.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
        {
            _logger.LogWarning("Failed to delete object {Key} from Cloudflare R2", key);
        }
    }

    public async Task<long?> GetObjectSize(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        try
        {
            var s3Client = GetS3Client();
            var key = GetServerFileName(sharedFileDefinition, partNumber);

            var request = new GetObjectMetadataRequest
            {
                BucketName = _cloudflareR2Settings.BucketName,
                Key = key
            };

            var response = await s3Client.GetObjectMetadataAsync(request);
            return response.ContentLength;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private AmazonS3Client GetS3Client()
    {
        if (_s3Client == null)
        {
            _s3Client = new AmazonS3Client(
                _cloudflareR2Settings.AccessKeyId,
                _cloudflareR2Settings.SecretAccessKey,
                new AmazonS3Config
                {
                    ServiceURL = _cloudflareR2Settings.Endpoint,
                    ForcePathStyle = true // Required for Cloudflare R2
                });
        }

        return _s3Client;
    }

    private string GetServerFileName(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        return sharedFileDefinition.SessionId + "_" + sharedFileDefinition.ClientInstanceId + "_" +
               sharedFileDefinition.GetFileName(partNumber);
    }
} 