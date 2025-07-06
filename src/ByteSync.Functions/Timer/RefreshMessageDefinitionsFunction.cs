using ByteSync.ServerCommon.Business.Messages;
using ByteSync.ServerCommon.Interfaces.Loaders;
using ByteSync.ServerCommon.Interfaces.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.Timer;

public class RefreshMessageDefinitionsFunction
{
    private readonly IMessageDefinitionsLoader _loader;
    private readonly IMessageDefinitionRepository _repository;
    private readonly ILogger<RefreshMessageDefinitionsFunction> _logger;

    public RefreshMessageDefinitionsFunction(IMessageDefinitionsLoader loader, IMessageDefinitionRepository repository,
        ILogger<RefreshMessageDefinitionsFunction> logger)
    {
        _loader = loader;
        _repository = repository;
        _logger = logger;
    }

    [Function("RefreshMessageDefinitionsFunction")]
    public async Task<int> RunAsync([TimerTrigger("0 0 */2 * * *"
#if DEBUG
        , RunOnStartup = true
#endif
        )] TimerInfo timerInfo)
    {
        _logger.LogInformation("Refreshing message definitions at: {Now}", DateTime.UtcNow);

        var messageDefinitions = await _loader.Load();
        var validMessageDefinitions = messageDefinitions.Where(d => d.EndDate > DateTime.UtcNow).ToList();

        await _repository.SaveAll(validMessageDefinitions);

        return validMessageDefinitions.Count;
    }
}
