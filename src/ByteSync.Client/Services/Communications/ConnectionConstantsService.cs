using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Arguments;
using ByteSync.Business.Misc;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Services.Communications;
using Microsoft.Extensions.Configuration;

namespace ByteSync.Services.Communications;

class ConnectionConstantsService : IConnectionConstantsService
{
    private string? _apiUrl;
    private SemaphoreSlim _semaphore = new(1, 1);
    
    private readonly IEnvironmentService _environmentService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConnectionConstantsService> _logger;

    public ConnectionConstantsService(IEnvironmentService environmentService, IConfiguration configuration,
        ILogger<ConnectionConstantsService> logger)
    {
        _environmentService = environmentService;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<string> GetApiUrl()
    {
        try
        {
            await _semaphore.WaitAsync();
            
            if (_apiUrl == null)
            {
                _apiUrl = SetApiUrl();
                _logger.LogInformation("API URL set to {ApiUrl}", _apiUrl);
            }

            return _apiUrl;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private string SetApiUrl()
    {
        if (_environmentService.ExecutionMode == ExecutionMode.Regular)
        {
            return _configuration["ProductionUrl"]!;
        }
        else
        {
            if (_environmentService.Arguments.Contains(RegularArguments.CF_API_URL_LOCAL_DEBUG))
            {
                return _configuration["LocalDebugUrl"]!;
            }
            else if (_environmentService.Arguments.Contains(RegularArguments.CF_API_URL_DEVELOPMENT))
            {
                return _configuration["DevelopmentUrl"]!;
            }
            else if (_environmentService.Arguments.Contains(RegularArguments.CF_API_URL_STAGING))
            {
                return _configuration["StagingUrl"]!;
            }
            else if (_environmentService.Arguments.Contains(RegularArguments.CF_API_URL_PRODUCTION))
            {
                return _configuration["ProductionUrl"]!;
            }
            else
            {
                return _configuration["DevelopmentUrl"]!;
            }
        }
    }

    public TimeSpan[] GetRetriesTimeSpans()
    {
        var timeSpans = new List<TimeSpan>();

        timeSpans.Add(TimeSpan.Zero);
        timeSpans.Add(TimeSpan.FromSeconds(2));
        timeSpans.Add(TimeSpan.FromSeconds(5));
        timeSpans.Add(TimeSpan.FromSeconds(10));
        timeSpans.Add(TimeSpan.FromSeconds(10));
        timeSpans.Add(TimeSpan.FromSeconds(10));
        timeSpans.Add(TimeSpan.FromSeconds(30));
        timeSpans.Add(TimeSpan.FromSeconds(30));
        timeSpans.Add(TimeSpan.FromSeconds(30));
        timeSpans.Add(TimeSpan.FromMinutes(1));
        timeSpans.Add(TimeSpan.FromMinutes(1));
        timeSpans.Add(TimeSpan.FromMinutes(1));
        timeSpans.Add(TimeSpan.FromMinutes(1));
        timeSpans.Add(TimeSpan.FromMinutes(1));
        timeSpans.Add(TimeSpan.FromMinutes(5));
        timeSpans.Add(TimeSpan.FromMinutes(5));
        timeSpans.Add(TimeSpan.FromMinutes(5));
        timeSpans.Add(TimeSpan.FromMinutes(5));
        timeSpans.Add(TimeSpan.FromMinutes(5));

        return timeSpans.ToArray();
    }
}