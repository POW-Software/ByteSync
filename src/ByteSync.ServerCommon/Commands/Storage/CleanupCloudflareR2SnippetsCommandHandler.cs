using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Commands.Storage;

public class CleanupCloudflareR2SnippetsCommandHandler : IRequestHandler<CleanupCloudflareR2SnippetsRequest, int>
{
    private readonly ICloudflareR2Service _cloudflareR2Service;
    private readonly ILogger<CleanupCloudflareR2SnippetsCommandHandler> _logger;
    private readonly CloudflareR2Settings _cloudflareR2Settings;

    public CleanupCloudflareR2SnippetsCommandHandler(
        ICloudflareR2Service cloudflareR2Service,
        IOptions<CloudflareR2Settings> cloudflareR2Settings,
        ILogger<CleanupCloudflareR2SnippetsCommandHandler> logger)
    {
        _cloudflareR2Service = cloudflareR2Service;
        _cloudflareR2Settings = cloudflareR2Settings.Value;
        _logger = logger;
    }

    public async Task<int> Handle(CleanupCloudflareR2SnippetsRequest request, CancellationToken cancellationToken)
    {
        if (_cloudflareR2Settings.RetentionDurationInDays < 1)
        {
            _logger.LogWarning("RetentionDurationInDays is less than 1, no element deleted");
            return 0;
        }

        var deletedObjectsCount = 0;
        var cutoffDate = DateTime.UtcNow.AddDays(-_cloudflareR2Settings.RetentionDurationInDays);

        var allObjects = await _cloudflareR2Service.GetAllObjects(cancellationToken);

        foreach (var obj in allObjects)
        {
            if (obj.Value <= cutoffDate)
            {
                _logger.LogInformation("Deleting obsolete R2 object {ObjectKey} (LastModified:{LastModified})", obj.Key, obj.Value);
                await _cloudflareR2Service.DeleteObjectByKey(obj.Key, cancellationToken);
                deletedObjectsCount += 1;
            }
        }

        return deletedObjectsCount;
    }
} 