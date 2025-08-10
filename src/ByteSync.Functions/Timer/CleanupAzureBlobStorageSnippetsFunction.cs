using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ByteSync.ServerCommon.Commands.Storage;
using Microsoft.Azure.Functions.Worker;

namespace ByteSync.Functions.Timer;

public class CleanupAzureBlobStorageSnippetsFunction
{
    private readonly ILogger<CleanupAzureBlobStorageSnippetsFunction> _logger;
    private readonly IMediator _mediator;

    public CleanupAzureBlobStorageSnippetsFunction(IConfiguration configuration, IMediator mediator, 
        ILogger<CleanupAzureBlobStorageSnippetsFunction> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    [Function("CleanupBlobFilesFunction")]
    public async Task<int> RunAsync([TimerTrigger("0 0 0 * * *" 
#if DEBUG
        , RunOnStartup= true
#endif
        )] TimerInfo myTimer)
    {
        _logger.LogInformation("Cleanup Azure BlobStorage Function started at: {Now}", DateTime.Now);
        var deletedBlobsCount = await _mediator.Send(new CleanupAzureBlobStorageSnippetsRequest());
        _logger.LogInformation("Cleanup Azure BlobStorage Function - Deletion complete, {Deleted} element(s)", deletedBlobsCount);
        return deletedBlobsCount;
    }
}