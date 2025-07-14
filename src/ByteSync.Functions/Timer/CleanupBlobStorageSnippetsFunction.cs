using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ByteSync.ServerCommon.Commands.Storage;
using Microsoft.Azure.Functions.Worker;

namespace ByteSync.Functions.Timer;

public class CleanupBlobStorageSnippetsFunction
{
    private readonly ILogger<CleanupBlobStorageSnippetsFunction> _logger;
    private readonly IMediator _mediator;

    public CleanupBlobStorageSnippetsFunction(IConfiguration configuration, IMediator mediator, 
        ILogger<CleanupBlobStorageSnippetsFunction> logger)
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
        _logger.LogInformation("Cleanup function executed at: {Now}", DateTime.Now);
        var deletedBlobsCount = await _mediator.Send(new CleanupBlobStorageSnippetsRequest());
        _logger.LogInformation("...Deletion complete, {Deleted} element(s)", deletedBlobsCount);
        return deletedBlobsCount;
    }
}