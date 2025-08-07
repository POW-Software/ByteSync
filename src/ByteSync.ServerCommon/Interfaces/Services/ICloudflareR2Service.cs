using Amazon.S3.Model;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ICloudflareR2Service
{
    Task<ListObjectsV2Response> ListObjectsAsync(ListObjectsV2Request request, CancellationToken cancellationToken);
    
    Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken cancellationToken);
} 