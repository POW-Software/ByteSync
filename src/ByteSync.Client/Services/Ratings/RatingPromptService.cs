using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
    internal const double PromptProbability = 1d / 3d;

    private const string StoreRatingUrl = "https://apps.microsoft.com/detail/9p17gqw3z2q2?hl=fr-FR&gl=FR";

    private static readonly IReadOnlyList<string> AdditionalRatingUrls = new[]
    {
        "https://github.com/POW-Software/ByteSync",
        "https://alternativeto.net/software/bytesync/about/",
        "https://www.majorgeeks.com/files/details/bytesync.html"
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

        return _randomValueProvider() < PromptProbability;
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
            default:
                break;
        }
    }

    private IEnumerable<RatingOption> BuildRatingOptions()
    {
        var options = new List<RatingOption>
        {
            new RatingOption(_localizationService[nameof(Resources.RatingPrompt_Channel_MicrosoftStore)], StoreRatingUrl)
        };

        if (_environmentService.DeploymentMode != DeploymentModes.MsixInstallation)
        {
            options.Add(new RatingOption(_localizationService[nameof(Resources.RatingPrompt_Channel_GitHub)], AdditionalRatingUrls[0]));
            options.Add(new RatingOption(_localizationService[nameof(Resources.RatingPrompt_Channel_AlternativeTo)], AdditionalRatingUrls[1]));
            options.Add(new RatingOption(_localizationService[nameof(Resources.RatingPrompt_Channel_MajorGeeks)], AdditionalRatingUrls[2]));
        }

        return options;
    }
}
