using System.Globalization;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business.Misc;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Services;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Ratings;
using ByteSync.ViewModels.Ratings;

namespace ByteSync.Services.Ratings;

public class RatingPromptService : IRatingPromptService, IDisposable
{
    private readonly ISynchronizationService _synchronizationService;
    private readonly IEnvironmentService _environmentService;
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;
    private readonly IDialogService _dialogService;
    private readonly IWebAccessor _webAccessor;
    private readonly ILocalizationService _localizationService;
    private readonly IRatingPromptConfigurationProvider _configurationProvider;
    private readonly ILogger<RatingPromptService> _logger;
    private readonly Func<double> _randomValueProvider;
    private readonly IDisposable _subscription;
    
    public RatingPromptService(ISynchronizationService synchronizationService, IEnvironmentService environmentService,
        IApplicationSettingsRepository applicationSettingsRepository, IDialogService dialogService,
        IWebAccessor webAccessor, ILocalizationService localizationService,
        IRatingPromptConfigurationProvider configurationProvider, ILogger<RatingPromptService> logger,
        Func<double>? randomValueProvider = null)
    {
        _synchronizationService = synchronizationService;
        _environmentService = environmentService;
        _applicationSettingsRepository = applicationSettingsRepository;
        _dialogService = dialogService;
        _webAccessor = webAccessor;
        _localizationService = localizationService;
        _configurationProvider = configurationProvider;
        _logger = logger;
        _randomValueProvider = randomValueProvider ?? (() => Random.Shared.NextDouble());
        
        _subscription = _synchronizationService.SynchronizationProcessData.SynchronizationEnd
            .Where(end => end != null)
            .Select(end => end!)
            .Subscribe(HandleSynchronizationEnd);
    }
    
    public void Dispose()
    {
        _subscription.Dispose();
    }
    
    private void HandleSynchronizationEnd(SynchronizationEnd synchronizationEnd)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (!ShouldPrompt(synchronizationEnd))
                {
                    return;
                }
                
                await ShowPromptAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while handling rating prompt");
            }
        });
    }
    
    private bool ShouldPrompt(SynchronizationEnd synchronizationEnd)
    {
        if (_environmentService.OperationMode != OperationMode.GraphicalUserInterface)
        {
            return false;
        }
        
        if (_environmentService.OperateCommandLine || _environmentService.IsAutoRunProfile())
        {
            return false;
        }
        
        if (synchronizationEnd.Status != SynchronizationEndStatuses.Regular)
        {
            return false;
        }
        
        var progress = _synchronizationService.SynchronizationProcessData.SynchronizationProgress.Value;
        if (progress != null && progress.ErrorActionsCount > 0)
        {
            return false;
        }
        
        var settings = _applicationSettingsRepository.GetCurrentApplicationSettings();
        if (settings.UserRatingOptOut)
        {
            return false;
        }
        
        return _randomValueProvider() < _configurationProvider.Configuration.PromptProbability;
    }
    
    private async Task ShowPromptAsync()
    {
        var options = BuildRatingOptions();
        var ratingPromptViewModel = new RatingPromptViewModel(_localizationService, options);
        
        _dialogService.ShowFlyout(nameof(Resources.RatingPrompt_Title), false, ratingPromptViewModel);
        
        var result = await ratingPromptViewModel.WaitForResultAsync();
        var timestamp = DateTimeOffset.UtcNow;
        
        switch (result.ResultType)
        {
            case RatingPromptResultType.Rate when result.Url != null:
                _applicationSettingsRepository.UpdateCurrentApplicationSettings(settings =>
                {
                    settings.UserRatingLastPromptedOn = timestamp;
                });
                await _webAccessor.OpenUrl(result.Url);
                
                break;
            case RatingPromptResultType.DoNotAskAgain:
                _applicationSettingsRepository.UpdateCurrentApplicationSettings(settings =>
                {
                    settings.UserRatingOptOut = true;
                    settings.UserRatingLastPromptedOn = timestamp;
                });
                
                break;
        }
    }
    
    private List<RatingOption> BuildRatingOptions()
    {
        var options = new List<RatingOption>();
        var configuration = _configurationProvider.Configuration;
        
        if (_environmentService.DeploymentMode == DeploymentModes.MsixInstallation)
        {
            var storeChannel = configuration.GetStoreChannel(_environmentService.OSPlatform);
            if (storeChannel != null)
            {
                options.Add(CreateRatingOption(storeChannel));
            }
        }
        
        AddChannels(options, configuration.AlwaysInclude);
        
        if (_environmentService.DeploymentMode != DeploymentModes.MsixInstallation)
        {
            var additionalChannels = GetRandomizedChannels(configuration.Additional)
                .Take(Math.Min(configuration.AdditionalCount, configuration.Additional.Count));
            AddChannels(options, additionalChannels);
        }
        
        return options;
    }
    
    private RatingOption CreateRatingOption(RatingPromptChannelConfiguration channel)
    {
        return new RatingOption(ResolveLabel(channel), channel.Url, channel.Icon);
    }
    
    private void AddChannels(ICollection<RatingOption> options, IEnumerable<RatingPromptChannelConfiguration> channels)
    {
        foreach (var channel in channels)
        {
            options.Add(CreateRatingOption(channel));
        }
    }
    
    private string ResolveLabel(RatingPromptChannelConfiguration channel)
    {
        var culture = _localizationService.CurrentCultureDefinition?.CultureInfo;
        
        return ResolveLabel(channel, culture);
    }
    
    private static string ResolveLabel(RatingPromptChannelConfiguration channel, CultureInfo? culture)
    {
        if (culture != null)
        {
            if (channel.Labels.TryGetValue(culture.Name, out var exactLabel))
            {
                return exactLabel;
            }
            
            var neutralCulture = culture.TwoLetterISOLanguageName;
            if (!string.IsNullOrWhiteSpace(neutralCulture)
                && channel.Labels.TryGetValue(neutralCulture, out var neutralLabel))
            {
                return neutralLabel;
            }
        }
        
        if (channel.Labels.TryGetValue("en", out var englishLabel))
        {
            return englishLabel;
        }
        
        return GetDomainFallback(channel.Url);
    }
    
    private static string GetDomainFallback(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            return url;
        }
        
        var host = uri.Host;
        if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            host = host[4..];
        }
        
        var hostParts = host.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var domain = hostParts.Length >= 2 ? hostParts[^2] : host;
        var readable = domain.Replace('-', ' ');
        
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(readable);
    }
    
    private IEnumerable<RatingPromptChannelConfiguration> GetRandomizedChannels(IEnumerable<RatingPromptChannelConfiguration> channels)
    {
        return channels
            .Select((channel, index) => new { Channel = channel, SortKey = _randomValueProvider(), Index = index })
            .OrderBy(entry => entry.SortKey)
            .ThenBy(entry => entry.Index)
            .Select(entry => entry.Channel);
    }
}