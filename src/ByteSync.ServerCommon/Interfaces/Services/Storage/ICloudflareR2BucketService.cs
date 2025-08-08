using Amazon.S3.Model;

namespace ByteSync.ServerCommon.Interfaces.Services.Storage;

public interface ICloudflareR2BucketService
{
    Task<ListObjectsV2Response> ListObjectsAsync(ListObjectsV2Request request, CancellationToken cancellationToken);
    
    Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken cancellationToken);
} 