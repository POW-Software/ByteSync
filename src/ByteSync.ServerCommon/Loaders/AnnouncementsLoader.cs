using ByteSync.Common.Controls.Json;
using ByteSync.Common.Business.Announcements;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Loaders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace ByteSync.ServerCommon.Loaders;

public class AnnouncementsLoader : IAnnouncementsLoader
{
    private readonly AppSettings _appSettings;
    private readonly ILogger<AnnouncementsLoader> _logger;
    private readonly HttpClient _httpClient;

    public AnnouncementsLoader(
        IOptions<AppSettings> appSettings,
        ILogger<AnnouncementsLoader> logger,
        HttpClient httpClient)
    {
        _appSettings = appSettings.Value;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<List<Announcement>> Load()
    {
        List<Announcement>? announcements = null;

        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(3 * (retryAttempt + 1)));

        await policy.Execute(async () =>
        {
            _logger.LogInformation("Loading announcements from {url}", _appSettings.AnnouncementsUrl);
            var contents = await _httpClient.GetStringAsync(_appSettings.AnnouncementsUrl);

            announcements = JsonHelper.Deserialize<List<Announcement>>(contents);
        });

        if (announcements == null)
        {
            throw new Exception("Failed to load announcements");
        }

        return announcements!;
    }
}
