using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ByteSync.Business.Configurations;
using ByteSync.Business.Misc;
using ByteSync.Common.Business.Misc;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Services.Ratings;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.Ratings;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
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
            .Callback<string, bool, FlyoutElementViewModel>((_, _, vm) =>
            {
                _promptShown.TrySetResult((RatingPromptViewModel)vm);
            });
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
        viewModel.SelectRateOption(viewModel.RatingOptions.First().Url);

        await WaitForCompletion(urlsOpened.Task);
        openedUrls.Should().ContainSingle(url => url.Contains("apps.microsoft.com"));
    }

    [Test]
    public async Task Opens_all_links_for_non_msix_installations()
    {
        var openedUrls = new List<string>();
        var urlsOpened = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _webAccessor.Setup(w => w.OpenUrl(It.IsAny<string>()))
            .Returns<string>(url =>
            {
                openedUrls.Add(url);
                if (openedUrls.Count == 4)
                {
                    urlsOpened.TrySetResult(true);
                }

                return Task.CompletedTask;
            });

        _environmentService.SetupGet(e => e.DeploymentMode).Returns(DeploymentModes.SetupInstallation);

        using var service = BuildService(() => 0.0);
        PublishSynchronizationEnd();

        var viewModel = await WaitForPromptAsync();
        viewModel.RatingOptions.Count.Should().Be(4);
        viewModel.SelectRateOption(viewModel.RatingOptions.Last().Url);

        await WaitForCompletion(urlsOpened.Task);
        openedUrls.Should().HaveCount(1);
        openedUrls.Should().Contain(url => url.Contains("apps.microsoft.com"));
        openedUrls.Should().Contain(url => url.Contains("majorgeeks.com"));
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

    private RatingPromptService BuildService(Func<double>? randomProvider)
    {
        return new RatingPromptService(_synchronizationService.Object, _environmentService.Object,
            _applicationSettingsRepository.Object, _dialogService.Object,
            _webAccessor.Object, _localizationService.Object, _logger.Object, randomProvider);
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
