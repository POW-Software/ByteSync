using ByteSync.Business.Configurations;
using ByteSync.Business.Misc;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Ratings;
using ByteSync.Services.Ratings;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.Ratings;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Ratings;

public class RatingPromptServiceTests
{
    private SynchronizationProcessData _synchronizationProcessData = null!;
    private Mock<ISynchronizationService> _synchronizationService = null!;
    private Mock<IEnvironmentService> _environmentService = null!;
    private Mock<IApplicationSettingsRepository> _applicationSettingsRepository = null!;
    private Mock<IDialogService> _dialogService = null!;
    private Mock<IWebAccessor> _webAccessor = null!;
    private Mock<ILocalizationService> _localizationService = null!;
    private ApplicationSettings _applicationSettings = null!;
    private Mock<ILogger<RatingPromptService>> _logger = null!;
    private TaskCompletionSource<RatingPromptViewModel> _promptShown = null!;
    
    [SetUp]
    public void SetUp()
    {
        _synchronizationProcessData = new SynchronizationProcessData();
        _synchronizationService = new Mock<ISynchronizationService>();
        _synchronizationService.SetupGet(s => s.SynchronizationProcessData).Returns(_synchronizationProcessData);
        
        _environmentService = new Mock<IEnvironmentService>();
        _environmentService.SetupGet(e => e.OperationMode).Returns(OperationMode.GraphicalUserInterface);
        _environmentService.SetupGet(e => e.OperateCommandLine).Returns(false);
        _environmentService.Setup(e => e.IsAutoRunProfile()).Returns(false);
        _environmentService.SetupGet(e => e.DeploymentMode).Returns(DeploymentModes.SetupInstallation);
        _environmentService.SetupGet(e => e.OSPlatform).Returns(OSPlatforms.Windows);
        
        _applicationSettings = new ApplicationSettings
        {
            UserRatingOptOut = false,
            UserRatingLastPromptedOn = null
        };
        
        _applicationSettingsRepository = new Mock<IApplicationSettingsRepository>();
        _applicationSettingsRepository.Setup(r => r.GetCurrentApplicationSettings()).Returns(() => _applicationSettings);
        _applicationSettingsRepository.Setup(r =>
                r.UpdateCurrentApplicationSettings(It.IsAny<Action<ApplicationSettings>>(), It.IsAny<bool>()))
            .Returns<Action<ApplicationSettings>, bool>((handler, _) =>
            {
                handler(_applicationSettings);
                
                return _applicationSettings;
            });
        
        _localizationService = new Mock<ILocalizationService>();
        _localizationService.Setup(ls => ls[It.IsAny<string>()]).Returns((string key) => key);
        
        _dialogService = new Mock<IDialogService>();
        _webAccessor = new Mock<IWebAccessor>();
        _logger = new Mock<ILogger<RatingPromptService>>();
        
        _promptShown = new TaskCompletionSource<RatingPromptViewModel>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dialogService.Setup(d => d.ShowFlyout(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<FlyoutElementViewModel>()))
            .Callback<string, bool, FlyoutElementViewModel>((_, _, vm) => { _promptShown.TrySetResult((RatingPromptViewModel)vm); });
    }
    
    [Test]
    public async Task Shows_prompt_on_successful_synchronization()
    {
        using var service = BuildService(() => 0.1);
        
        PublishSynchronizationEnd();
        
        var viewModel = await WaitForPromptAsync();
        viewModel.SelectAskLater();
    }
    
    [Test]
    public async Task Does_not_prompt_when_opt_out_is_enabled()
    {
        _applicationSettings.UserRatingOptOut = true;
        
        using var service = BuildService(() => 0.1);
        
        PublishSynchronizationEnd();
        
        await EnsurePromptNotShownAsync();
    }
    
    [Test]
    public async Task Does_not_prompt_when_probability_fails_or_in_command_line_mode()
    {
        using var serviceLowProbability = BuildService(() => 0.9);
        PublishSynchronizationEnd();
        await EnsurePromptNotShownAsync();
        
        _environmentService.SetupGet(e => e.OperationMode).Returns(OperationMode.CommandLine);
        
        using var serviceCli = BuildService(() => 0.1);
        PublishSynchronizationEnd();
        await EnsurePromptNotShownAsync();
    }
    
    [Test]
    public async Task Does_not_prompt_when_synchronization_has_errors_or_failed_status()
    {
        _synchronizationProcessData.SynchronizationProgress.OnNext(new SynchronizationProgress
        {
            ErrorActionsCount = 1
        });
        
        using var serviceWithErrors = BuildService(() => 0.1);
        PublishSynchronizationEnd();
        await EnsurePromptNotShownAsync();
        
        using var serviceWithFailedStatus = BuildService(() => 0.1);
        PublishSynchronizationEnd(SynchronizationEndStatuses.Error);
        await EnsurePromptNotShownAsync();
    }
    
    [Test]
    public async Task Opens_store_link_only_for_msix()
    {
        var openedUrls = new List<string>();
        var urlsOpened = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _webAccessor.Setup(w => w.OpenUrl(It.IsAny<string>()))
            .Returns<string>(url =>
            {
                openedUrls.Add(url);
                urlsOpened.TrySetResult(true);
                
                return Task.CompletedTask;
            });
        
        _environmentService.SetupGet(e => e.DeploymentMode).Returns(DeploymentModes.MsixInstallation);
        
        using var service = BuildService(() => 0.0);
        PublishSynchronizationEnd();
        
        var viewModel = await WaitForPromptAsync();
        viewModel.RatingOptions.Should().HaveCount(2);
        viewModel.RatingOptions[0].Url.Should().Contain("apps.microsoft.com");
        viewModel.RatingOptions[1].Url.Should().Contain("github.com");
        viewModel.SelectRateOption(viewModel.RatingOptions.First().Url);
        
        await WaitForCompletion(urlsOpened.Task);
        openedUrls.Should().ContainSingle(url => url.Contains("apps.microsoft.com"));
    }
    
    [Test]
    public async Task Opens_selected_link_for_non_msix_installations()
    {
        var openedUrls = new List<string>();
        var urlsOpened = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _webAccessor.Setup(w => w.OpenUrl(It.IsAny<string>()))
            .Returns<string>(url =>
            {
                openedUrls.Add(url);
                urlsOpened.TrySetResult(true);
                
                return Task.CompletedTask;
            });
        
        _environmentService.SetupGet(e => e.DeploymentMode).Returns(DeploymentModes.SetupInstallation);
        
        using var service = BuildService(() => 0.0);
        PublishSynchronizationEnd();
        
        var viewModel = await WaitForPromptAsync();
        viewModel.RatingOptions.Should().HaveCount(4);
        viewModel.RatingOptions.First().Url.Should().Contain("github.com");
        var allowedDomains = new[]
        {
            "github.com",
            "alternativeto.net",
            "majorgeeks.com",
            "softpedia.com",
            "uptodown.com",
            "sourceforge.net"
        };
        viewModel.RatingOptions.Select(option => option.Url)
            .Should()
            .OnlyContain(url => allowedDomains.Any(domain => url.Contains(domain)));
        viewModel.RatingOptions.Select(option => option.Url).Distinct().Should().HaveCount(4);
        
        var selectedUrl = viewModel.RatingOptions.First(option => option.Url.Contains("github.com")).Url;
        viewModel.SelectRateOption(selectedUrl);
        
        await WaitForCompletion(urlsOpened.Task);
        openedUrls.Should().HaveCount(1);
        openedUrls.Should().Contain(url => url.Contains("github.com"));
    }
    
    [Test]
    public async Task Stores_opt_out_when_user_requests_no_more_prompts()
    {
        var settingsUpdated = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _applicationSettingsRepository.Setup(r =>
                r.UpdateCurrentApplicationSettings(It.IsAny<Action<ApplicationSettings>>(), It.IsAny<bool>()))
            .Returns<Action<ApplicationSettings>, bool>((handler, _) =>
            {
                handler(_applicationSettings);
                settingsUpdated.TrySetResult(true);
                
                return _applicationSettings;
            });
        
        using var service = BuildService(() => 0.0);
        
        PublishSynchronizationEnd();
        
        var viewModel = await WaitForPromptAsync();
        viewModel.SelectDoNotAskAgain();
        
        await WaitForCompletion(settingsUpdated.Task);
        _applicationSettings.UserRatingOptOut.Should().BeTrue();
        _applicationSettings.UserRatingLastPromptedOn.Should().NotBeNull();
    }
    
    private RatingPromptService BuildService(Func<double>? randomProvider, RatingPromptConfiguration? configuration = null)
    {
        var configurationProvider = new StubRatingPromptConfigurationProvider(configuration ?? BuildConfiguration());
        
        return new RatingPromptService(_synchronizationService.Object, _environmentService.Object,
            _applicationSettingsRepository.Object, _dialogService.Object,
            _webAccessor.Object, _localizationService.Object, configurationProvider, _logger.Object, randomProvider);
    }
    
    private static RatingPromptConfiguration BuildConfiguration(int additionalCount = 3)
    {
        var alwaysInclude = new List<RatingPromptChannelConfiguration>
        {
            new("RatingPrompt_Channel_GitHub", "https://github.com/POW-Software/ByteSync", "LogosGithub")
        };
        var additional = new List<RatingPromptChannelConfiguration>
        {
            new("RatingPrompt_Channel_AlternativeTo", "https://alternativeto.net/software/bytesync/about/", "RegularWorld"),
            new("RatingPrompt_Channel_MajorGeeks", "https://www.majorgeeks.com/files/details/bytesync.html", "RegularWorld"),
            new("RatingPrompt_Channel_Softpedia", "https://www.softpedia.com/get/System/Back-Up-and-Recovery/ByteSync.shtml",
                "RegularWorld"),
            new("RatingPrompt_Channel_Uptodown", "https://bytesync-windows.fr.uptodown.com/windows", "RegularWorld"),
            new("RatingPrompt_Channel_SourceForge", "https://sourceforge.net/projects/bytesync/", "RegularWorld")
        };
        var stores = new Dictionary<OSPlatforms, RatingPromptChannelConfiguration>
        {
            [OSPlatforms.Windows] = new RatingPromptChannelConfiguration(
                "RatingPrompt_Channel_MicrosoftStore",
                "https://apps.microsoft.com/detail/9p17gqw3z2q2?hl=fr-FR&gl=FR",
                "RegularStore")
        };
        
        return new RatingPromptConfiguration(1d / 3d, additionalCount, alwaysInclude, additional, stores);
    }
    
    private sealed class StubRatingPromptConfigurationProvider : IRatingPromptConfigurationProvider
    {
        public StubRatingPromptConfigurationProvider(RatingPromptConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        public RatingPromptConfiguration Configuration { get; }
    }
    
    private void PublishSynchronizationEnd(SynchronizationEndStatuses status = SynchronizationEndStatuses.Regular)
    {
        _synchronizationProcessData.SynchronizationEnd.OnNext(new SynchronizationEnd
        {
            SessionId = "session-id",
            FinishedOn = DateTimeOffset.UtcNow,
            Status = status
        });
    }
    
    private async Task<RatingPromptViewModel> WaitForPromptAsync()
    {
        var completed = await Task.WhenAny(_promptShown.Task, Task.Delay(TimeSpan.FromSeconds(1)));
        completed.Should().Be(_promptShown.Task);
        
        return _promptShown.Task.Result;
    }
    
    private async Task EnsurePromptNotShownAsync()
    {
        var completed = await Task.WhenAny(_promptShown.Task, Task.Delay(TimeSpan.FromMilliseconds(200))) == _promptShown.Task;
        completed.Should().BeFalse();
    }
    
    private static async Task WaitForCompletion(Task task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(1))) == task;
        completed.Should().BeTrue();
    }
}