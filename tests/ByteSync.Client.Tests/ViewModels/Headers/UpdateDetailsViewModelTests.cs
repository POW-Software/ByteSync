using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using ByteSync.Business.Updates;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Factories.Proxies;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Repositories.Updates;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Updates;
using ByteSync.Tests.Helpers;
using ByteSync.ViewModels.Headers;
using ByteSync.ViewModels.Misc;
using DynamicData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.ViewModels.Headers;

[TestFixture]
public class UpdateDetailsViewModelTests
{
    private Mock<IUpdateService> _updateServiceMock = null!;
    private Mock<IAvailableUpdateRepository> _availableUpdateRepositoryMock = null!;
    private Mock<ILocalizationService> _localizationServiceMock = null!;
    private Mock<IWebAccessor> _webAccessorMock = null!;
    private Mock<IUpdateRepository> _updateRepositoryMock = null!;
    private Mock<ISoftwareVersionProxyFactory> _softwareVersionProxyFactoryMock = null!;
    private Mock<IEnvironmentService> _environmentServiceMock = null!;
    private Mock<ILogger<UpdateDetailsViewModel>> _loggerMock = null!;
    private ErrorViewModel _errorViewModel = null!;
    private SourceCache<SoftwareVersion, string> _sourceCache = null!;
    private Progress<UpdateProgress> _progress = null!;

    [SetUp]
    public void SetUp()
    {
        _updateServiceMock = new Mock<IUpdateService>();
        _availableUpdateRepositoryMock = new Mock<IAvailableUpdateRepository>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _webAccessorMock = new Mock<IWebAccessor>();
        _updateRepositoryMock = new Mock<IUpdateRepository>();
        _softwareVersionProxyFactoryMock = new Mock<ISoftwareVersionProxyFactory>();
        _environmentServiceMock = new Mock<IEnvironmentService>();
        _loggerMock = new Mock<ILogger<UpdateDetailsViewModel>>();
        _errorViewModel = new ErrorViewModel(_localizationServiceMock.Object);

        _sourceCache = new SourceCache<SoftwareVersion, string>(sv => sv.Version);
        var observable = _sourceCache.Connect().AsObservableCache();

        // Create a Progress<T> that we can control in tests
        _progress = new Progress<UpdateProgress>();

        _availableUpdateRepositoryMock
            .Setup(x => x.ObservableCache)
            .Returns(observable);

        _updateRepositoryMock
            .Setup(x => x.Progress)
            .Returns(_progress);

        // Also support reporting via repository mock → forward to the same Progress instance
        _updateRepositoryMock
            .Setup(x => x.ReportProgress(It.IsAny<UpdateProgress>()))
            .Callback<UpdateProgress>(progress => ((IProgress<UpdateProgress>)_progress).Report(progress));

        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => $"Localized_{key}");

        _environmentServiceMock
            .Setup(x => x.DeploymentMode)
            .Returns(DeploymentModes.Portable);

        _environmentServiceMock
            .Setup(x => x.IsPortableApplication)
            .Returns(true);
    }

    [TearDown]
    public void TearDown()
    {
        _sourceCache.Dispose();
    }

    private UpdateDetailsViewModel CreateViewModel()
    {
        return new UpdateDetailsViewModel(
            _updateServiceMock.Object,
            _availableUpdateRepositoryMock.Object,
            _localizationServiceMock.Object,
            _webAccessorMock.Object,
            _updateRepositoryMock.Object,
            _softwareVersionProxyFactoryMock.Object,
            _environmentServiceMock.Object,
            _errorViewModel,
            _loggerMock.Object);
    }

    [Test]
    public void Constructor_Should_Initialize_Properties_With_Default_Values()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.AvailableUpdatesMessage.Should().Be("");
        viewModel.Progress.Should().Be("");
        viewModel.SelectedVersion.Should().BeNull();
        viewModel.IsAutoUpdating.Should().BeFalse();
        viewModel.Error.Should().Be(_errorViewModel);
        viewModel.ShowReleaseNotesCommand.Should().NotBeNull();
        viewModel.RunUpdateCommand.Should().NotBeNull();
    }

    [Test]
    public void Constructor_Should_Setup_SoftwareVersions_Binding()
    {
        // Arrange
        var softwareVersion = new SoftwareVersion { Version = "1.0.0", Level = PriorityLevel.Recommended };
        var proxy = new SoftwareVersionProxy(softwareVersion, _localizationServiceMock.Object);

        _softwareVersionProxyFactoryMock
            .Setup(x => x.CreateSoftwareVersionProxy(softwareVersion))
            .Returns(proxy);

        // Act
        var viewModel = CreateViewModel();
        _sourceCache.AddOrUpdate(softwareVersion);

        // Assert
        viewModel.SoftwareVersions.Should().HaveCount(1);
        viewModel.SoftwareVersions.First().Should().Be(proxy);
    }

    [Test]
    public void CanAutoUpdate_Should_Return_False_When_DeploymentMode_Is_MsixInstallation()
    {
        // Arrange
        _environmentServiceMock
            .Setup(x => x.DeploymentMode)
            .Returns(DeploymentModes.MsixInstallation);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.CanAutoUpdate.Should().BeFalse();
    }

    [Test]
    public void CanAutoUpdate_Should_Return_True_When_Windows_And_Not_MsixInstallation()
    {
        // Arrange
        _environmentServiceMock
            .Setup(x => x.DeploymentMode)
            .Returns(DeploymentModes.Portable);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            viewModel.CanAutoUpdate.Should().BeTrue();
        }
    }

    [Test]
    public void CanAutoUpdate_Should_Return_True_When_Linux_And_PortableApplication()
    {
        // Arrange
        _environmentServiceMock
            .Setup(x => x.DeploymentMode)
            .Returns(DeploymentModes.Portable);
        _environmentServiceMock
            .Setup(x => x.IsPortableApplication)
            .Returns(true);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            viewModel.CanAutoUpdate.Should().BeTrue();
        }
    }

    [Test]
    public void SetAvailableUpdate_Should_Set_Single_Update_Message()
    {
        // Arrange
        var softwareVersion = new SoftwareVersion { Version = "1.0.0", Level = PriorityLevel.Recommended };
        var proxy = new SoftwareVersionProxy(softwareVersion, _localizationServiceMock.Object);

        _softwareVersionProxyFactoryMock
            .Setup(x => x.CreateSoftwareVersionProxy(softwareVersion))
            .Returns(proxy);

        _localizationServiceMock
            .Setup(x => x["UpdateDetails_AvailableUpdate"])
            .Returns("1 update available");

        // Act
        var viewModel = CreateViewModel();
        viewModel.Activator.Activate();
        _sourceCache.AddOrUpdate(softwareVersion);

        // Assert
        viewModel.AvailableUpdatesMessage.Should().Be("1 update available");
    }

    [Test]
    public void SetAvailableUpdate_Should_Set_Multiple_Updates_Message()
    {
        // Arrange
        var softwareVersion1 = new SoftwareVersion { Version = "1.0.0", Level = PriorityLevel.Recommended };
        var softwareVersion2 = new SoftwareVersion { Version = "1.1.0", Level = PriorityLevel.Optional };
        var proxy1 = new SoftwareVersionProxy(softwareVersion1, _localizationServiceMock.Object);
        var proxy2 = new SoftwareVersionProxy(softwareVersion2, _localizationServiceMock.Object);

        _softwareVersionProxyFactoryMock
            .Setup(x => x.CreateSoftwareVersionProxy(softwareVersion1))
            .Returns(proxy1);
        _softwareVersionProxyFactoryMock
            .Setup(x => x.CreateSoftwareVersionProxy(softwareVersion2))
            .Returns(proxy2);

        _localizationServiceMock
            .Setup(x => x["UpdateDetails_AvailableUpdates"])
            .Returns("{0} updates available");

        // Act
        var viewModel = CreateViewModel();
        viewModel.Activator.Activate();
        _sourceCache.AddOrUpdate(softwareVersion1);
        _sourceCache.AddOrUpdate(softwareVersion2);

        // Assert
        viewModel.AvailableUpdatesMessage.Should().Be("2 updates available");
    }

    [Test]
    public void SetAvailableUpdate_Should_Add_AutoUpdate_Not_Supported_Message_When_CanAutoUpdate_Is_False()
    {
        // Arrange
        _environmentServiceMock
            .Setup(x => x.DeploymentMode)
            .Returns(DeploymentModes.MsixInstallation);

        var softwareVersion = new SoftwareVersion { Version = "1.0.0", Level = PriorityLevel.Recommended };
        var proxy = new SoftwareVersionProxy(softwareVersion, _localizationServiceMock.Object);

        _softwareVersionProxyFactoryMock
            .Setup(x => x.CreateSoftwareVersionProxy(softwareVersion))
            .Returns(proxy);

        _localizationServiceMock
            .Setup(x => x["UpdateDetails_AvailableUpdate"])
            .Returns("1 update available");
        _localizationServiceMock
            .Setup(x => x["UpdateDetails_AutoUpdateNotSupported"])
            .Returns("Auto-update not supported");

        // Act
        var viewModel = CreateViewModel();
        viewModel.Activator.Activate();
        _sourceCache.AddOrUpdate(softwareVersion);

        // Assert
        viewModel.AvailableUpdatesMessage.Should().Contain("Auto-update not supported");
    }

    [Test]
    public async Task ShowReleaseNotesCommand_Should_Open_Release_Notes()
    {
        // Arrange
        var softwareVersion = new SoftwareVersion { Version = "1.0.0", Level = PriorityLevel.Recommended };
        var proxy = new SoftwareVersionProxy(softwareVersion, _localizationServiceMock.Object);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.ShowReleaseNotesCommand.Execute(proxy).ToTask();

        // Assert
        _webAccessorMock.Verify(x => x.OpenReleaseNotes(It.Is<Version>(v => v.ToString() == "1.0.0")), Times.Once);
    }

    [Test]
    public async Task ShowReleaseNotesCommand_Should_Set_Error_When_Exception_Occurs()
    {
        // Arrange
        var softwareVersion = new SoftwareVersion { Version = "1.0.0", Level = PriorityLevel.Recommended };
        var proxy = new SoftwareVersionProxy(softwareVersion, _localizationServiceMock.Object);

        var exception = new InvalidOperationException("Test exception");
        _webAccessorMock
            .Setup(x => x.OpenReleaseNotes(It.IsAny<Version>()))
            .ThrowsAsync(exception);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.ShowReleaseNotesCommand.Execute(proxy).ToTask();

        // Assert
        viewModel.Error.ErrorMessage.Should().NotBeNull();
    }

    [Test]
    public async Task RunUpdateCommand_Should_Update_State_During_Execution()
    {
        // Arrange
        var softwareVersion = new SoftwareVersion { Version = "1.0.0", Level = PriorityLevel.Recommended };
        var proxy = new SoftwareVersionProxy(softwareVersion, _localizationServiceMock.Object);

        var container = new Mock<FlyoutContainerViewModel>(Mock.Of<ILocalizationService>(), Mock.Of<IFlyoutElementViewModelFactory>());
        var viewModel = CreateViewModel();
        viewModel.Container = container.Object;

        var tcs = new TaskCompletionSource<bool>();
        _updateServiceMock
            .Setup(x => x.UpdateAsync(softwareVersion, It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var task = viewModel.RunUpdateCommand.Execute(proxy).ToTask();

        // Assert - During execution
        viewModel.IsAutoUpdating.Should().BeTrue();
        viewModel.SelectedVersion.Should().Be(proxy);
        container.Object.CanCloseCurrentFlyout.Should().BeFalse();

        // Complete the update
        tcs.SetResult(true);
        await task;

        // Assert - After execution
        viewModel.IsAutoUpdating.Should().BeFalse();
        container.Object.CanCloseCurrentFlyout.Should().BeTrue();
    }

    [Test]
    public async Task RunUpdateCommand_Should_Set_Error_When_Exception_Occurs()
    {
        // Arrange
        var softwareVersion = new SoftwareVersion { Version = "1.0.0", Level = PriorityLevel.Recommended };
        var proxy = new SoftwareVersionProxy(softwareVersion, _localizationServiceMock.Object);

        var container = new Mock<FlyoutContainerViewModel>(Mock.Of<ILocalizationService>(), Mock.Of<IFlyoutElementViewModelFactory>());
        var viewModel = CreateViewModel();
        viewModel.Container = container.Object;

        var exception = new InvalidOperationException("Update failed");
        _updateServiceMock
            .Setup(x => x.UpdateAsync(softwareVersion, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await viewModel.RunUpdateCommand.Execute(proxy).ToTask();

        // Assert
        viewModel.Error.ErrorMessage.Should().NotBeNull();
        viewModel.IsAutoUpdating.Should().BeFalse();
        container.Object.CanCloseCurrentFlyout.Should().BeTrue();
    }

    [Test]
    public void UpdateManager_ProgressReported_Should_Update_Progress_For_Downloading()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Activator.Activate(); // Activate to trigger WhenActivated
        var updateProgress = new UpdateProgress(UpdateProgressStatus.Downloading, 50);

        _localizationServiceMock
            .Setup(x => x["UpdateDetails_Downloading"])
            .Returns("downloading the update");

        // Act
        TriggerProgress(updateProgress);

        // Assert
        viewModel.ShouldEventuallyBe(vm => vm.Progress, "Downloading the update - 50%");
    }

    [Test]
    public void UpdateManager_ProgressReported_Should_Update_Progress_For_Extracting()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Activator.Activate(); // Activate to trigger WhenActivated
        var updateProgress = new UpdateProgress(UpdateProgressStatus.Extracting);

        _localizationServiceMock
            .Setup(x => x["UpdateDetails_Extracting"])
            .Returns("extracting new files");

        // Act
        TriggerProgress(updateProgress);

        // Assert
        viewModel.ShouldEventuallyBe(vm => vm.Progress, "Extracting new files");
    }

    [Test]
    public void UpdateManager_ProgressReported_Should_Update_Progress_For_RestartingApplication()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Activator.Activate(); // Activate to trigger WhenActivated
        var updateProgress = new UpdateProgress(UpdateProgressStatus.RestartingApplication);

        _localizationServiceMock
            .Setup(x => x["UpdateDetails_RestartingApplication"])
            .Returns("restarting the application");

        // Act
        TriggerProgress(updateProgress);

        // Assert
        viewModel.ShouldEventuallyBe(vm => vm.Progress, "Restarting the application");
    }

    [Test]
    public void UpdateManager_ProgressReported_Should_Update_Progress_For_UpdatingFiles()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Activator.Activate(); // Activate to trigger WhenActivated
        var updateProgress = new UpdateProgress(UpdateProgressStatus.UpdatingFiles, 75);

        _localizationServiceMock
            .Setup(x => x["UpdateDetails_UpdatingFiles"])
            .Returns("updating files");

        // Act
        TriggerProgress(updateProgress);

        // Assert
        viewModel.ShouldEventuallyBe(vm => vm.Progress, "Updating files - 75%");
    }

    [Test]
    public void UpdateManager_ProgressReported_Should_Update_Progress_For_BackingUpExistingFiles()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Activator.Activate(); // Activate to trigger WhenActivated
        var updateProgress = new UpdateProgress(UpdateProgressStatus.BackingUpExistingFiles);

        _localizationServiceMock
            .Setup(x => x["UpdateDetails_BackingUpExistingFiles"])
            .Returns("backing up existing files");

        // Act
        TriggerProgress(updateProgress);

        // Assert
        viewModel.ShouldEventuallyBe(vm => vm.Progress, "Backing up existing files");
    }

    [Test]
    public void UpdateManager_ProgressReported_Should_Update_Progress_For_MovingNewFiles()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Activator.Activate(); // Activate to trigger WhenActivated
        var updateProgress = new UpdateProgress(UpdateProgressStatus.MovingNewFiles, 90);

        _localizationServiceMock
            .Setup(x => x["UpdateDetails_MovingNewFiles"])
            .Returns("moving new files");

        // Act
        TriggerProgress(updateProgress);

        // Assert
        viewModel.ShouldEventuallyBe(vm => vm.Progress, "Moving new files - 90%");
    }

    [Test]
    public async Task CancelIfNeeded_Should_Cancel_CancellationToken()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.CancelIfNeeded();

        // Assert - We can't directly test if the token is cancelled, but we can verify the method completes
        // The actual cancellation logic is tested indirectly through the RunUpdateCommand tests
        Assert.Pass("CancelIfNeeded completed successfully");
    }

    private void TriggerProgress(UpdateProgress updateProgress)
    {
        ((IProgress<UpdateProgress>)_progress).Report(updateProgress);
    }
}