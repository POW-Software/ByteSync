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
using ByteSync.ViewModels.Ratings;

namespace ByteSync.Services.Ratings;

public class RatingPromptService : IRatingPromptService, IDisposable
{
    internal const double PROMPT_PROBABILITY = 1d / 3d;
    
#pragma warning disable S1075
    private const string STORE_RATING_URL = "https://apps.microsoft.com/detail/9p17gqw3z2q2?hl=fr-FR&gl=FR";
    private const string GITHUB_RATING_URL = "https://github.com/POW-Software/ByteSync";
    private const string ALTERNATIVETO_RATING_URL = "https://alternativeto.net/software/bytesync/about/";
    private const string MAJOR_GEEKS_RATING_URL = "https://www.majorgeeks.com/files/details/bytesync.html";
    private const string SOFTPEDIA_RATING_URL = "https://www.softpedia.com/get/System/Back-Up-and-Recovery/ByteSync.shtml";
    private const string UPTODOWN_RATING_URL = "https://bytesync-windows.fr.uptodown.com/windows";
    private const string SOURCE_FORGE_RATING_URL = "https://sourceforge.net/projects/bytesync/";
#pragma warning restore S1075
    
    private const int RANDOM_ADDITIONAL_OPTIONS_COUNT = 3;
    private const string RATING_PROMPT_CHANNEL_SOFTPEDIA_KEY = "RatingPrompt_Channel_Softpedia";
    private const string RATING_PROMPT_CHANNEL_UPTODOWN_KEY = "RatingPrompt_Channel_Uptodown";
    private const string RATING_PROMPT_CHANNEL_SOURCE_FORGE_KEY = "RatingPrompt_Channel_SourceForge";
    private const string REGULAR_WORLD_ICON = "RegularWorld";
    
    private static readonly RatingChannel StoreRatingChannel = new(
        nameof(Resources.RatingPrompt_Channel_MicrosoftStore),
        STORE_RATING_URL,
        "RegularStore");
    
    private static readonly RatingChannel GitHubRatingChannel = new(
        nameof(Resources.RatingPrompt_Channel_GitHub),
        GITHUB_RATING_URL,
        "LogosGithub");
    
    private static readonly RatingChannel[] AdditionalRatingChannels =
    {
        new RatingChannel(
            nameof(Resources.RatingPrompt_Channel_AlternativeTo),
            ALTERNATIVETO_RATING_URL,
            REGULAR_WORLD_ICON),
        new RatingChannel(
            nameof(Resources.RatingPrompt_Channel_MajorGeeks),
            MAJOR_GEEKS_RATING_URL,
            REGULAR_WORLD_ICON),
        new RatingChannel(
            RATING_PROMPT_CHANNEL_SOFTPEDIA_KEY,
            SOFTPEDIA_RATING_URL,
            REGULAR_WORLD_ICON),
        new RatingChannel(
            RATING_PROMPT_CHANNEL_UPTODOWN_KEY,
            UPTODOWN_RATING_URL,
            REGULAR_WORLD_ICON),
        new RatingChannel(
            RATING_PROMPT_CHANNEL_SOURCE_FORGE_KEY,
            SOURCE_FORGE_RATING_URL,
            REGULAR_WORLD_ICON)
    };
    
    private readonly ISynchronizationService _synchronizationService;
    private readonly IEnvironmentService _environmentService;
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;
    private readonly IDialogService _dialogService;
    private readonly IWebAccessor _webAccessor;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<RatingPromptService> _logger;
    private readonly Func<double> _randomValueProvider;
    private readonly IDisposable _subscription;
    
    public RatingPromptService(ISynchronizationService synchronizationService, IEnvironmentService environmentService,
        IApplicationSettingsRepository applicationSettingsRepository, IDialogService dialogService,
        IWebAccessor webAccessor, ILocalizationService localizationService, ILogger<RatingPromptService> logger,
        Func<double>? randomValueProvider = null)
    {
        _synchronizationService = synchronizationService;
        _environmentService = environmentService;
        _applicationSettingsRepository = applicationSettingsRepository;
        _dialogService = dialogService;
        _webAccessor = webAccessor;
        _localizationService = localizationService;
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
        
        return _randomValueProvider() < PROMPT_PROBABILITY;
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
        
        if (_environmentService.DeploymentMode == DeploymentModes.MsixInstallation)
        {
            options.Add(CreateRatingOption(StoreRatingChannel));
            options.Add(CreateRatingOption(GitHubRatingChannel));
        }
        else
        {
            options.Add(CreateRatingOption(GitHubRatingChannel));
            
            var additionalChannels = GetRandomizedChannels(AdditionalRatingChannels)
                .Take(Math.Min(RANDOM_ADDITIONAL_OPTIONS_COUNT, AdditionalRatingChannels.Length));
            foreach (var channel in additionalChannels)
            {
                options.Add(CreateRatingOption(channel));
            }
        }
        
        return options;
    }
    
    private RatingOption CreateRatingOption(RatingChannel channel)
    {
        return new RatingOption(_localizationService[channel.LabelKey], channel.Url, channel.Icon);
    }
    
    private IEnumerable<RatingChannel> GetRandomizedChannels(IEnumerable<RatingChannel> channels)
    {
        return channels
            .Select((channel, index) => new { Channel = channel, SortKey = _randomValueProvider(), Index = index })
            .OrderBy(entry => entry.SortKey)
            .ThenBy(entry => entry.Index)
            .Select(entry => entry.Channel);
    }
    
    private sealed record RatingChannel(string LabelKey, string Url, string Icon);
}