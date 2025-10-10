using System.Text;
using Avalonia.Input.Platform;
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
using Microsoft.Extensions.Logging;
using Moq;

namespace ByteSync.Client.IntegrationTests.ViewModels.TrustedNetworks;

public class AddTrustedClientViewModel_HeadlessTests : HeadlessIntegrationTest
{
    private Mock<IPublicKeysManager> _publicKeysManager = null!;
    private Mock<IApplicationSettingsRepository> _appSettings = null!;
    private Mock<IPublicKeysTruster> _truster = null!;
    private Mock<ILogger<AddTrustedClientViewModel>> _logger = null!;
    
    [SetUp]
    public void Setup()
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
    
    private static PublicKeyCheckData CreateCheckData() => new()
    {
        IssuerPublicKeyInfo = new PublicKeyInfo { ClientId = "OtherClient", PublicKey = Encoding.UTF8.GetBytes("OTHER_PUBLIC_KEY") },
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
        var vm = new AddTrustedClientViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object,
            _truster.Object, _logger.Object, null!)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false }
        };
        
        var closed = false;
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
    public async Task ValidateClient_Waits_For_OtherParty_Then_Closes_On_UI()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(false, true); // OtherPartyHasFinished = false initially
        
        var vm = new AddTrustedClientViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object,
            _truster.Object, _logger.Object, null!)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false }
        };
        
        var closed = false;
        vm.CloseFlyoutRequested += (_, _) => closed = true;
        
        // Truster marks my party checked only; other party not yet finished
        _truster.Setup(t => t.OnPublicKeyValidationFinished(It.IsAny<PublicKeyCheckData>(), trustParams, true))
            .Returns(() =>
            {
                trustParams.PeerTrustProcessData.SetMyPartyChecked(true);
                
                return Task.CompletedTask;
            });
        
        await ExecuteOnUiThread(() =>
        {
            vm.Container.IsFlyoutContainerVisible = true;
            vm.ValidateClientCommand.Execute().Subscribe();
            
            return Task.CompletedTask;
        });
        
        // Simulate the remote party finishing shortly after
        _ = Task.Run(async () =>
        {
            await Task.Delay(200);
            trustParams.PeerTrustProcessData.SetOtherPartyChecked(true);
        });
        
        // Wait for close request (will include the internal 3s delay after success)
        PumpUntil(() => closed, timeoutMs: 10000);
        
        closed.Should().BeTrue();
        vm.Container.CanCloseCurrentFlyout.Should().BeTrue();
    }
    
    [Test]
    public async Task RejectClient_Should_ShowError_Then_Close_On_UI()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(false, false);
        
        var vm = new AddTrustedClientViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object,
            _truster.Object, _logger.Object, null!)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false }
        };
        
        var closed = false;
        vm.CloseFlyoutRequested += (_, _) => closed = true;
        
        _truster.Setup(t => t.OnPublicKeyValidationFinished(It.IsAny<PublicKeyCheckData>(), trustParams, false))
            .Returns(Task.CompletedTask);
        _truster.Setup(t => t.OnPublicKeyValidationCanceled(It.IsAny<PublicKeyCheckData>(), trustParams))
            .Returns(Task.CompletedTask);
        
        await ExecuteOnUiThread(() =>
        {
            vm.Container.IsFlyoutContainerVisible = true;
            vm.RejectClientCommand.Execute().Subscribe();
            
            return Task.CompletedTask;
        });
        
        // Attendre que le flag de fermeture soit positionné (après délai interne et callbacks)
        PumpUntil(() => closed);
        
        closed.Should().BeTrue();
        vm.Container.CanCloseCurrentFlyout.Should().BeTrue();
        _truster.Verify(t => t.OnPublicKeyValidationFinished(It.IsAny<PublicKeyCheckData>(), trustParams, false), Times.Once);
        _truster.Verify(t => t.OnPublicKeyValidationCanceled(It.IsAny<PublicKeyCheckData>(), trustParams), Times.Once);
    }
    
    [Test]
    public async Task CopyToClipboard_WhenClipboardNull_Should_Set_ErrorFlags()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(false, true);
        
        var vm = new AddTrustedClientViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object,
            _truster.Object, _logger.Object, null!)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false },
            SafetyKeyParts = ["alpha", "beta"]
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
        
        var vm = new AddTrustedClientViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object,
            _truster.Object, _logger.Object, null!)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false },
            SafetyKeyParts = ["alpha", "beta"]
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
    
    [Test]
    public async Task ValidateClient_Exception_Should_Be_Caught_And_Not_Close()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(false, true);
        
        var vm = new AddTrustedClientViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object,
            _truster.Object, _logger.Object, null!)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false }
        };
        
        var closed = false;
        vm.CloseFlyoutRequested += (_, _) => closed = true;
        
        // Force a synchronous exception inside ValidateClient at the first await
        _truster.Setup(t => t.OnPublicKeyValidationFinished(It.IsAny<PublicKeyCheckData>(), trustParams, true))
            .Throws(new InvalidOperationException("boom-validate"));
        
        var completed = false;
        await ExecuteOnUiThread(() =>
        {
            vm.Container.IsFlyoutContainerVisible = true;
            vm.ValidateClientCommand.Execute().Subscribe(_ => { }, _ => { }, () => completed = true);
            
            return Task.CompletedTask;
        });
        
        PumpUntil(() => completed);
        
        closed.Should().BeFalse();
        vm.Container.CanCloseCurrentFlyout.Should().BeFalse();
        vm.ShowSuccess.Should().BeFalse();
        vm.ShowError.Should().BeFalse();
        
        _logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ValidateClient")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()!
        ), Times.AtLeastOnce);
    }
    
    [Test]
    public async Task RejectClient_Exception_Should_Be_Caught_And_Not_Close()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(false, false);
        
        var vm = new AddTrustedClientViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object,
            _truster.Object, _logger.Object, null!)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false }
        };
        
        var closed = false;
        vm.CloseFlyoutRequested += (_, _) => closed = true;
        
        // Throw synchronously on first truster call to enter catch early
        _truster.Setup(t => t.OnPublicKeyValidationFinished(It.IsAny<PublicKeyCheckData>(), trustParams, false))
            .Throws(new InvalidOperationException("boom-reject"));
        
        // Ensure cancel is NOT called due to early exception
        _truster.Setup(t => t.OnPublicKeyValidationCanceled(It.IsAny<PublicKeyCheckData>(), trustParams))
            .Throws(new Exception("should-not-be-called"));
        
        var completed = false;
        await ExecuteOnUiThread(() =>
        {
            vm.Container.IsFlyoutContainerVisible = true;
            vm.RejectClientCommand.Execute().Subscribe(_ => { }, _ => { }, () => completed = true);
            
            return Task.CompletedTask;
        });
        
        PumpUntil(() => completed);
        
        closed.Should().BeFalse();
        vm.Container.CanCloseCurrentFlyout.Should().BeFalse();
        vm.ShowError.Should().BeFalse();
        
        _logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("RejectClient")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()!
        ), Times.AtLeastOnce);
        
        _truster.Verify(t => t.OnPublicKeyValidationCanceled(It.IsAny<PublicKeyCheckData>(), trustParams), Times.Never);
    }
    
    [Test]
    public async Task CancelCommand_Should_Call_Truster_Cancel_And_Not_Close()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(false, false);
        
        var vm = new AddTrustedClientViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object,
            _truster.Object, _logger.Object, null!)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false }
        };
        
        var closed = false;
        vm.CloseFlyoutRequested += (_, _) => closed = true;
        
        _truster.Setup(t => t.OnPublicKeyValidationCanceled(It.IsAny<PublicKeyCheckData>(), trustParams))
            .Returns(Task.CompletedTask);
        
        var completed = false;
        await ExecuteOnUiThread(() =>
        {
            vm.Container.IsFlyoutContainerVisible = true;
            vm.CancelCommand.Execute().Subscribe(_ => { }, _ => { }, () => completed = true);
            
            return Task.CompletedTask;
        });
        
        PumpUntil(() => completed);
        
        _truster.Verify(t => t.OnPublicKeyValidationCanceled(It.IsAny<PublicKeyCheckData>(), trustParams), Times.Once);
        _truster.Verify(t => t.OnPublicKeyValidationFinished(It.IsAny<PublicKeyCheckData>(), trustParams, It.IsAny<bool>()), Times.Never);
        closed.Should().BeFalse();
        vm.Container.CanCloseCurrentFlyout.Should().BeFalse();
    }
    
    [Test]
    public async Task CopyToClipboard_Exception_Should_Set_ErrorFlags()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(false, true);
        
        var vm = new ThrowingClipboardViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object,
            _truster.Object, _logger.Object)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false },
            SafetyKeyParts = null!
        };
        
        await ExecuteOnUiThread(async () =>
        {
            vm.CopyToClipboardCommand.Execute().Subscribe();
            await Task.CompletedTask;
        });
        
        PumpUntil(() => vm.IsCopyToClipboardOK || vm.IsClipboardCheckError);
        
        vm.IsCopyToClipboardOK.Should().BeFalse();
        vm.IsClipboardCheckError.Should().BeTrue();
        
        _logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CopyToClipboard error")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()
        ), Times.AtLeastOnce);
    }
    
    [Test]
    public async Task CheckClipboard_Exception_Should_Set_ErrorFlags()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(false, true);
        
        var vm = new ThrowingClipboardViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object,
            _truster.Object, _logger.Object)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false },
            SafetyKeyParts = null!
        };
        
        await ExecuteOnUiThread(async () =>
        {
            vm.CheckClipboardCommand.Execute().Subscribe();
            await Task.CompletedTask;
        });
        
        PumpUntil(() => vm.IsClipboardCheckOK || vm.IsClipboardCheckError);
        
        vm.IsClipboardCheckOK.Should().BeFalse();
        vm.IsClipboardCheckError.Should().BeTrue();
        
        _logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CheckClipboard error")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()!
        ), Times.AtLeastOnce);
    }
    
    private sealed class ThrowingClipboardViewModel : AddTrustedClientViewModel
    {
        public ThrowingClipboardViewModel(PublicKeyCheckData publicKeyCheckData, TrustDataParameters trustDataParameters,
            IPublicKeysManager publicKeysManager, IApplicationSettingsRepository applicationSettingsManager,
            IPublicKeysTruster publicKeysTruster, ILogger<AddTrustedClientViewModel> logger)
            : base(publicKeyCheckData, trustDataParameters, publicKeysManager, applicationSettingsManager, publicKeysTruster, logger, null!)
        {
        }
        
        protected override IClipboard? GetClipboard()
        {
            throw new InvalidOperationException("clipboard-throw");
        }
    }
    
    private sealed class MockClipboardViewModel : AddTrustedClientViewModel
    {
        private readonly IClipboard _clipboard;
        
        public MockClipboardViewModel(PublicKeyCheckData publicKeyCheckData, TrustDataParameters trustDataParameters,
            IPublicKeysManager publicKeysManager, IApplicationSettingsRepository applicationSettingsManager,
            IPublicKeysTruster publicKeysTruster, ILogger<AddTrustedClientViewModel> logger, IClipboard clipboard)
            : base(publicKeyCheckData, trustDataParameters, publicKeysManager, applicationSettingsManager, publicKeysTruster, logger, null!)
        {
            _clipboard = clipboard;
        }
        
        protected override IClipboard GetClipboard() => _clipboard;
    }
    
    [Test]
    public async Task CopyToClipboard_WhenClipboardAvailable_Should_Set_OKFlags()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(false, true);
        
        var clipboard = new Mock<IClipboard>();
        clipboard.Setup(c => c.SetTextAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        
        var vm = new MockClipboardViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object,
            _truster.Object, _logger.Object, clipboard.Object)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false },
            SafetyKeyParts = ["alpha", "beta"]
        };
        
        await ExecuteOnUiThread(async () =>
        {
            vm.CopyToClipboardCommand.Execute().Subscribe();
            await Task.CompletedTask;
        });
        PumpUntil(() => vm.IsCopyToClipboardOK || vm.IsClipboardCheckError);
        
        vm.IsCopyToClipboardOK.Should().BeTrue();
        vm.IsClipboardCheckError.Should().BeFalse();
        clipboard.Verify(c => c.SetTextAsync("alpha beta"), Times.Once);
    }
    
    [Test]
    public async Task CheckClipboard_WhenClipboardAvailable_Matching_Should_Set_OK()
    {
        var check = CreateCheckData();
        var trustParams = CreateTrustParams(false, true);
        
        var clipboard = new Mock<IClipboard>();
        clipboard.Setup(c => c.GetTextAsync()).Returns(Task.FromResult("alpha beta")!);
        
        var vm = new MockClipboardViewModel(check, trustParams, _publicKeysManager.Object, _appSettings.Object,
            _truster.Object, _logger.Object, clipboard.Object)
        {
            Container = new FlyoutContainerViewModel { CanCloseCurrentFlyout = false },
            SafetyKeyParts = ["alpha", "beta"]
        };
        
        await ExecuteOnUiThread(async () =>
        {
            vm.CheckClipboardCommand.Execute().Subscribe();
            await Task.CompletedTask;
        });
        PumpUntil(() => vm.IsClipboardCheckOK || vm.IsClipboardCheckError);
        
        vm.IsClipboardCheckOK.Should().BeTrue();
        vm.IsClipboardCheckError.Should().BeFalse();
        clipboard.Verify(c => c.GetTextAsync(), Times.Once);
    }
}