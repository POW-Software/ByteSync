using System.Reflection;
using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Communications.Transfers.Uploading;

[TestFixture]
public class FileUploadProcessorAdjusterTests
{
    private Mock<ISlicerEncrypter> _mockSlicerEncrypter = null!;
    private Mock<ILogger<FileUploadProcessor>> _mockLogger = null!;
    private Mock<IFileUploadCoordinator> _mockCoordinator = null!;
    private Mock<IFileUploadWorker> _mockWorker = null!;
    private Mock<IFileTransferApiClient> _mockApi = null!;
    private Mock<ISessionService> _mockSessionService = null!;
    private Mock<IAdaptiveUploadController> _mockAdaptive = null!;
    private Mock<IUploadSlicingManager> _mockSlicingManager = null!;

    private SemaphoreSlim _stateSemaphore = null!;
    private SharedFileDefinition _file = null!;

    [SetUp]
    public void SetUp()
    {
        _mockSlicerEncrypter = new Mock<ISlicerEncrypter>();
        _mockLogger = new Mock<ILogger<FileUploadProcessor>>();
        _mockCoordinator = new Mock<IFileUploadCoordinator>();
        _mockWorker = new Mock<IFileUploadWorker>();
        _mockApi = new Mock<IFileTransferApiClient>();
        _mockSessionService = new Mock<ISessionService>();
        _mockAdaptive = new Mock<IAdaptiveUploadController>();
        _mockSlicingManager = new Mock<IUploadSlicingManager>();

        _file = new SharedFileDefinition { Id = "f1", SessionId = "s1", UploadedFileLength = 1024 };
        _stateSemaphore = new SemaphoreSlim(1, 1);

        // Coordinator default stubs
        _mockCoordinator.Setup(x => x.AvailableSlices).Returns(Channel.CreateBounded<FileUploaderSlice>(8));
        _mockCoordinator.Setup(x => x.WaitForCompletionAsync()).Returns(Task.CompletedTask);
    }

    private static void InvokePrivate(object target, string methodName, params object[] args)
    {
        var mi = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        mi.Should().NotBeNull();
        mi!.Invoke(target, args);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        fi.Should().NotBeNull();

        return (T)fi!.GetValue(target)!;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        fi.Should().NotBeNull();
        fi!.SetValue(target, value);
    }

    [Test]
    public void AdjustSlots_Increase_ReleasesExpectedPermits()
    {
        // Arrange: limiter starts at 0; grantedSlots defaults to 0
        var limiter = new SemaphoreSlim(0, 4);

        var proc = new FileUploadProcessor(
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _mockCoordinator.Object,
            _mockWorker.Object,
            _mockApi.Object,
            _mockSessionService.Object,
            "path",
            _stateSemaphore,
            _mockAdaptive.Object,
            _mockSlicingManager.Object,
            limiter);

        // Act: increase desired slots to 3
        InvokePrivate(proc, "AdjustSlots", 3);

        // Assert
        limiter.CurrentCount.Should().Be(3);
        GetPrivateField<int>(proc, "_grantedSlots").Should().Be(3);
    }

    [Test]
    public void AdjustSlots_Decrease_TakesExpectedPermits()
    {
        // Arrange: limiter has 3 permits, grantedSlots set to 3
        var limiter = new SemaphoreSlim(0, 4);
        limiter.Release(3);

        var proc = new FileUploadProcessor(
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _mockCoordinator.Object,
            _mockWorker.Object,
            _mockApi.Object,
            _mockSessionService.Object,
            "path",
            _stateSemaphore,
            _mockAdaptive.Object,
            _mockSlicingManager.Object,
            limiter);

        SetPrivateField(proc, "_grantedSlots", 3);

        // Act: reduce desired to 1
        InvokePrivate(proc, "AdjustSlots", 1);

        // Assert: two permits taken, leaving 1; grantedSlots updated accordingly
        limiter.CurrentCount.Should().Be(1);
        GetPrivateField<int>(proc, "_grantedSlots").Should().Be(1);
    }

    [Test]
    public void EnsureWorkers_StartsMissingWorkers()
    {
        // Arrange
        var limiter = new SemaphoreSlim(1, 4);
        var proc = new FileUploadProcessor(
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _mockCoordinator.Object,
            _mockWorker.Object,
            _mockApi.Object,
            _mockSessionService.Object,
            "path",
            _stateSemaphore,
            _mockAdaptive.Object,
            _mockSlicingManager.Object,
            limiter);

        // Set progress state and current started workers
        SetPrivateField(proc, "_progressState", new UploadProgressState());
        SetPrivateField(proc, "_startedWorkers", 1);

        // Act: ensure 3 desired workers
        InvokePrivate(proc, "EnsureWorkers", 3);

        // Assert: should start 2 additional workers
        _mockWorker.Verify(
            x => x.UploadAvailableSlicesAdaptiveAsync(It.IsAny<Channel<FileUploaderSlice>>(), It.IsAny<UploadProgressState>()),
            Times.Exactly(2));
    }
}