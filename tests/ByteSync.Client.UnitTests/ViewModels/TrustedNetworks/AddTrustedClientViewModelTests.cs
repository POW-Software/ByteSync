using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using ByteSync.Business;
using ByteSync.Business.Communications;
using ByteSync.Business.Communications.PublicKeysTrusting;
using ByteSync.Business.Configurations;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.TrustedNetworks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.TrustedNetworks;

[TestFixture]
public class AddTrustedClientViewModelTests
{
    private Mock<IPublicKeysManager> _publicKeysManager = null!;
    private Mock<IApplicationSettingsRepository> _appSettings = null!;
    private Mock<IPublicKeysTruster> _truster = null!;
    private Mock<ILogger<AddTrustedClientViewModel>> _logger = null!;

    private PublicKeyCheckData CreateCheckData()
    {
        var issuer = new PublicKeyInfo
        {
            ClientId = "OtherClient",
            PublicKey = Encoding.UTF8.GetBytes("OTHER_PUBLIC_KEY")
        };

        return new PublicKeyCheckData
        {
            IssuerPublicKeyInfo = issuer,
        };
    }

    [SetUp]
    public void SetUp()
    {
        _publicKeysManager = new Mock<IPublicKeysManager>();
        _appSettings = new Mock<IApplicationSettingsRepository>();
        _truster = new Mock<IPublicKeysTruster>();
        _logger = new Mock<ILogger<AddTrustedClientViewModel>>();

        _appSettings.Setup(a => a.GetCurrentApplicationSettings())
            .Returns(new ApplicationSettings { ClientId = "MyClient" });

        _publicKeysManager.Setup(m => m.GetMyPublicKeyInfo())
            .Returns(new PublicKeyInfo { ClientId = "MyClient", PublicKey = Encoding.UTF8.GetBytes("MY_PUBLIC_KEY") });

        _publicKeysManager.Setup(m => m.BuildTrustedPublicKey(It.IsAny<PublicKeyCheckData>()))
            .Returns((PublicKeyCheckData p) => new TrustedPublicKey
            {
                ClientId = p.IssuerPublicKeyInfo.ClientId,
                PublicKey = p.IssuerPublicKeyInfo.PublicKey,
                SafetyKey = new string('0', 64)
            });
    }

    private TrustDataParameters CreateTrustParams(out PeerTrustProcessData peer, bool otherFinished, bool success)
    {
        peer = new PeerTrustProcessData("OtherClient");
        if (otherFinished)
        {
            peer.SetOtherPartyChecked(success);
        }

        return new TrustDataParameters(0, 2, false, "S1", peer);
    }

    private TestableAddTrustedClientViewModel CreateVm(PublicKeyCheckData check, TrustDataParameters trustParams)
    {
        return new TestableAddTrustedClientViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object,
            _truster.Object, _logger.Object, null!);
    }

    [Test]
    public async Task ValidateClient_Success_Should_Trust_And_Close()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(out var peer, true, true);

        var vm = CreateVm(check, trustParams);
        vm.Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false };

        _truster.Setup(t => t.OnPublicKeyValidationFinished(It.IsAny<PublicKeyCheckData>(), trustParams, true))
            .Returns(async () =>
            {
                peer.SetMyPartyChecked(true);
                await Task.CompletedTask;
            });

        bool closeRequested = false;
        vm.CloseFlyoutRequested += (_, _) => closeRequested = true;

        await vm.ValidateClientCommand.Execute();

        _publicKeysManager.Verify(m => m.Trust(It.IsAny<TrustedPublicKey>()), Times.Once);
        vm.ShowSuccess.Should().BeFalse();
        vm.ShowError.Should().BeFalse();
        vm.Container.CanCloseCurrentFlyout.Should().BeTrue();
        closeRequested.Should().BeTrue();
    }

    [Test]
    public async Task ValidateClient_Failure_Should_ShowError_And_Close()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(out var peer, true, false);

        var vm = CreateVm(check, trustParams);
        vm.Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false };

        _truster.Setup(t => t.OnPublicKeyValidationFinished(It.IsAny<PublicKeyCheckData>(), trustParams, true))
            .Returns(async () =>
            {
                peer.SetMyPartyChecked(true);
                await Task.CompletedTask;
            });

        bool closeRequested = false;
        vm.CloseFlyoutRequested += (_, _) => closeRequested = true;

        await vm.ValidateClientCommand.Execute();

        _publicKeysManager.Verify(m => m.Trust(It.IsAny<TrustedPublicKey>()), Times.Never);
        vm.ShowError.Should().BeFalse();
        vm.Container.CanCloseCurrentFlyout.Should().BeTrue();
        closeRequested.Should().BeTrue();
    }

    [Test]
    public async Task RejectClient_Should_Call_Truster_Cancel_And_Close()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(out _, true, false);

        var vm = CreateVm(check, trustParams);
        vm.Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false };

        bool closeRequested = false;
        vm.CloseFlyoutRequested += (_, _) => closeRequested = true;

        await vm.RejectClientCommand.Execute();

        _truster.Verify(t => t.OnPublicKeyValidationFinished(It.IsAny<PublicKeyCheckData>(), trustParams, false), Times.Once);
        _truster.Verify(t => t.OnPublicKeyValidationCanceled(It.IsAny<PublicKeyCheckData>(), trustParams), Times.Once);
        vm.Container.CanCloseCurrentFlyout.Should().BeTrue();
        closeRequested.Should().BeTrue();
    }

    [Test]
    public void EmptyConstructor_Should_Work_Fine()
    {
        var vm = new AddTrustedClientViewModel();

        vm.Should().NotBeNull();
    }

    [Test]
    public void OnDisplayed_Should_Disable_Flyout_Closing()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(out _, true, true);

        var vm = CreateVm(check, trustParams);
        vm.Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = true };

        vm.OnDisplayed();

        vm.Container.CanCloseCurrentFlyout.Should().BeFalse();
    }

    [Test]
    public async Task WhenActivated_Toggles_CanExecute_While_Command_IsExecuting()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(out _, true, true);

        var vm = CreateVm(check, trustParams);
        vm.Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false };

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _truster.Setup(t => t.OnPublicKeyValidationCanceled(It.IsAny<PublicKeyCheckData>(), trustParams))
            .Returns(tcs.Task);

        vm.Activator.Activate();

        var canExecute = vm.CopyToClipboardCommand.CanExecute
            .Replay(1)
            .RefCount();

        (await canExecute.Take(1).Timeout(TimeSpan.FromSeconds(1)).ToTask()).Should().BeTrue();

        var canExecuteFalseTask = canExecute
            .FirstAsync(v => !v)
            .Timeout(TimeSpan.FromSeconds(1))
            .ToTask();

        var cancelTask = vm.CancelCommand.Execute().ToTask();

        (await canExecuteFalseTask).Should().BeFalse();

        var canExecuteTrueTask = canExecute
            .FirstAsync(v => v)
            .Timeout(TimeSpan.FromSeconds(1))
            .ToTask();

        tcs.SetResult();

        (await canExecuteTrueTask).Should().BeTrue();
        await cancelTask;
    }

    private class TestableAddTrustedClientViewModel : AddTrustedClientViewModel
    {
        public TestableAddTrustedClientViewModel(PublicKeyCheckData? publicKeyCheckData,
            TrustDataParameters trustDataParameters, IPublicKeysManager publicKeysManager,
            IApplicationSettingsRepository applicationSettingsManager, IPublicKeysTruster publicKeysTruster,
            ILogger<AddTrustedClientViewModel> logger, Views.MainWindow mainWindow)
            : base(publicKeyCheckData, trustDataParameters, publicKeysManager, applicationSettingsManager,
                publicKeysTruster, logger, mainWindow)
        {
        }

        protected override Task DelayAsync(TimeSpan delay) => Task.CompletedTask;
    }
}
