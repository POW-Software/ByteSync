using ByteSync.Business;
using ByteSync.Business.Communications;
using ByteSync.Business.Communications.PublicKeysTrusting;
using ByteSync.Business.Configurations;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.TrustedNetworks;
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.ViewModels.TrustedNetworks;

public class AddTrustedClientViewModel_HeadlessTests : HeadlessIntegrationTest
{
    private Mock<IPublicKeysManager> _publicKeysManager = null!;
    private Mock<IApplicationSettingsRepository> _appSettings = null!;
    private Mock<IPublicKeysTruster> _truster = null!;

    [SetUp]
    public void Setup()
    {
        _publicKeysManager = new Mock<IPublicKeysManager>();
        _appSettings = new Mock<IApplicationSettingsRepository>();
        _truster = new Mock<IPublicKeysTruster>();

        _appSettings.Setup(a => a.GetCurrentApplicationSettings())
            .Returns(new ApplicationSettings { ClientId = "MyClient" });

        _publicKeysManager.Setup(m => m.GetMyPublicKeyInfo())
            .Returns(new PublicKeyInfo { ClientId = "MyClient", PublicKey = System.Text.Encoding.UTF8.GetBytes("MY_PUBLIC_KEY") });

        _publicKeysManager.Setup(m => m.BuildTrustedPublicKey(It.IsAny<PublicKeyCheckData>()))
            .Returns((PublicKeyCheckData p) => new TrustedPublicKey
            {
                ClientId = p.IssuerPublicKeyInfo.ClientId,
                PublicKey = p.IssuerPublicKeyInfo.PublicKey,
                SafetyKey = new string('0', 64)
            });
    }

    private static PublicKeyCheckData CreateCheckData() => new()
    {
        IssuerPublicKeyInfo = new PublicKeyInfo { ClientId = "OtherClient", PublicKey = System.Text.Encoding.UTF8.GetBytes("OTHER_PUBLIC_KEY") },
    };

    private static TrustDataParameters CreateTrustParams(bool otherFinished, bool success)
    {
        var peer = new PeerTrustProcessData("OtherClient");
        if (otherFinished)
        {
            peer.SetOtherPartyChecked(success);
        }
        return new TrustDataParameters(0, 2, false, "S1", peer);
    }

    [Test]
    public async Task ValidateClient_Should_Toggle_Success_Then_Close_On_UI()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(false, true); // force async path waiting for other party
        // Avoid instantiating MainWindow in headless; pass a dummy via null-forgiving
        var vm = new AddTrustedClientViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object, _truster.Object, null!)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false }
        };

        bool closed = false;
        vm.CloseFlyoutRequested += (_, _) => closed = true;

        // Start the command on UI thread
        await ExecuteOnUiThread(() =>
        {
            vm.Container.IsFlyoutContainerVisible = true;
            _truster.Setup(t => t.OnPublicKeyValidationFinished(It.IsAny<PublicKeyCheckData>(), trustParams, true))
                .Returns(() =>
                {
                    trustParams.PeerTrustProcessData.SetMyPartyChecked(true);
                    trustParams.PeerTrustProcessData.SetOtherPartyChecked(true);
                    return Task.CompletedTask;
                });
            vm.ValidateClientCommand.Execute().Subscribe();
            return Task.CompletedTask;
        });
        // Pump UI until closed flag flips or timeout
        PumpUntil(() => closed);

        closed.Should().BeTrue();
        vm.Container.CanCloseCurrentFlyout.Should().BeTrue();
    }

    [Test]
    public async Task CopyToClipboard_WhenClipboardNull_Should_Set_ErrorFlags()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(false, true);

        var vm = new AddTrustedClientViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object, _truster.Object, null!)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false },
            SafetyKeyParts = new[] { "alpha", "beta" }
        };

        await ExecuteOnUiThread(async () =>
        {
            vm.CopyToClipboardCommand.Execute().Subscribe();
            await Task.CompletedTask;
        });
        PumpUntil(() => vm.IsCopyToClipboardOK || vm.IsClipboardCheckError);

        vm.IsCopyToClipboardOK.Should().BeFalse();
        vm.IsClipboardCheckError.Should().BeTrue();
    }

    [Test]
    public async Task CheckClipboard_WhenClipboardNull_Should_Set_ErrorFlags()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(false, true);

        var vm = new AddTrustedClientViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object, _truster.Object, null!)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false },
            SafetyKeyParts = new[] { "alpha", "beta" }
        };

        await ExecuteOnUiThread(async () =>
        {
            vm.CheckClipboardCommand.Execute().Subscribe();
            await Task.CompletedTask;
        });
        PumpUntil(() => vm.IsClipboardCheckOK || vm.IsClipboardCheckError);

        vm.IsClipboardCheckOK.Should().BeFalse();
        vm.IsClipboardCheckError.Should().BeTrue();
    }
}


