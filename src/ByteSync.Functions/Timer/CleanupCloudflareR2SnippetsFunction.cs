using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ByteSync.ServerCommon.Commands.Storage;
using Microsoft.Azure.Functions.Worker;

namespace ByteSync.Functions.Timer;

public class CleanupCloudflareR2SnippetsFunction
{
    private readonly ILogger<CleanupCloudflareR2SnippetsFunction> _logger;
    private readonly IMediator _mediator;

    public CleanupCloudflareR2SnippetsFunction(IConfiguration configuration, IMediator mediator, 
        ILogger<CleanupCloudflareR2SnippetsFunction> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    [Function("CleanupCloudflareR2FilesFunction")]
    public async Task<int> RunAsync([TimerTrigger("0 0 0 * * *" 
#if DEBUG
        , RunOnStartup= true
#endif
        )] TimerInfo myTimer)
    {
        _logger.LogInformation("Cleanup Cloudflare R2 Function started at: {Now}", DateTime.Now);
        var deletedObjectsCount = await _mediator.Send(new CleanupCloudflareR2SnippetsRequest());
        _logger.LogInformation("Cleanup Cloudflare R2 Function - Deletion complete, {Deleted} element(s)", deletedObjectsCount);
        return deletedObjectsCount;
    }
} 