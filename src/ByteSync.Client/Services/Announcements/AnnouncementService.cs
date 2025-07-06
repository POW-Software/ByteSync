using System.Threading;
using ByteSync.Interfaces.Announcements;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace ByteSync.Services.Announcements;

public class AnnouncementService : IAnnouncementService, IDisposable
{
    private readonly IAnnouncementApiClient _apiClient;
    private readonly IAnnouncementRepository _repository;
    private readonly ILogger<AnnouncementService> _logger;
    private CancellationTokenSource? _refreshCancellationTokenSource;

    public AnnouncementService(IAnnouncementApiClient apiClient, IAnnouncementRepository repository,
        ILogger<AnnouncementService> logger)
    {
        _apiClient = apiClient;
        _repository = repository;
        _logger = logger;
    }

    public async Task Start()
    {
        await RefreshAnnouncements();

        _refreshCancellationTokenSource = new CancellationTokenSource();
        var token = _refreshCancellationTokenSource.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromHours(2), token);
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    await RefreshAnnouncements();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while refreshing announcements");
                }
            }
        }, token);
    }

    private async Task RefreshAnnouncements()
    {
        try
        {
            var announcements = await _apiClient.GetAnnouncements();
            _repository.Clear();
            _repository.AddOrUpdate(announcements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading announcements");
        }
    }

    public void Dispose()
    {
        var cts = _refreshCancellationTokenSource;
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}
