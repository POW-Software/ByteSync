using ByteSync.ServerCommon.Business.Announcements;
using ByteSync.ServerCommon.Interfaces.Loaders;
using ByteSync.ServerCommon.Interfaces.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.Timer;

public class RefreshAnnouncementsFunction
{
    private readonly IAnnouncementsLoader _loader;
    private readonly IAnnouncementRepository _repository;
    private readonly ILogger<RefreshAnnouncementsFunction> _logger;

    public RefreshAnnouncementsFunction(IAnnouncementsLoader loader, IAnnouncementRepository repository,
        ILogger<RefreshAnnouncementsFunction> logger)
    {
        _loader = loader;
        _repository = repository;
        _logger = logger;
    }

    [Function("RefreshAnnouncementsFunction")]
    public async Task<int> RunAsync([TimerTrigger("0 0 */2 * * *"
#if DEBUG
        , RunOnStartup = true
#endif
        )] TimerInfo timerInfo)
    {
        var currentUtcTime = DateTime.UtcNow;
        _logger.LogInformation("Refreshing announcements at: {Now}", currentUtcTime);

        var announcements = await _loader.Load();
        var validAnnouncements = announcements.Where(d => d.EndDate > currentUtcTime).ToList();

        await _repository.SaveAll(validAnnouncements);

        return validAnnouncements.Count;
    }
}
