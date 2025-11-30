using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ByteSync.Common.Business.Announcements;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Services.Communications.Api;

public class AnnouncementApiClient : IAnnouncementApiClient
{
    private readonly IApiInvoker _apiInvoker;
    private readonly ILogger<AnnouncementApiClient> _logger;

    public AnnouncementApiClient(IApiInvoker apiInvoker, ILogger<AnnouncementApiClient> logger)
    {
        _apiInvoker = apiInvoker;
        _logger = logger;
    }

    public async Task<List<Announcement>> GetAnnouncements()
    {
        try
        {
            return await _apiInvoker.GetAsync<List<Announcement>>("announcements");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving announcements");
            throw;
        }
    }
}
