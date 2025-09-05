using Autofac;
using Autofac.Features.Indexed;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Factories;
using ByteSync.Interfaces;
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
public class FileUploadProcessorFactoryTests
{
    private ILifetimeScope _container = null!;
    private Mock<ISlicerEncrypter> _mockSlicerEncrypter = null!;
    private Mock<ILogger<FileUploadCoordinator>> _mockCoordinatorLogger = null!;
    private Mock<ILogger<FileSlicer>> _mockSlicerLogger = null!;
    private Mock<ILogger<FileUploadWorker>> _mockWorkerLogger = null!;
    private Mock<IPolicyFactory> _mockPolicyFactory = null!;
    private Mock<IFileTransferApiClient> _mockFileTransferApiClient = null!;
    private Mock<IIndex<StorageProvider, IUploadStrategy>> _mockStrategies = null!;
    private Mock<ISessionService> _mockSessionService = null!;
    private Mock<IAdaptiveUploadController> _mockAdaptiveController = null!;
    private Mock<IFileUploadProcessor> _mockProcessor = null!;
    private Mock<IUploadSlicingManager> _mockSlicingManager = null!;
    
    private FileUploadProcessorFactory _factory = null!;

    private SharedFileDefinition _sharedFileDefinition = null!;

    [SetUp]
    public void SetUp()
    {
        _mockSlicerEncrypter = new Mock<ISlicerEncrypter>();
        _mockCoordinatorLogger = new Mock<ILogger<FileUploadCoordinator>>();
        _mockSlicerLogger = new Mock<ILogger<FileSlicer>>();
        _mockWorkerLogger = new Mock<ILogger<FileUploadWorker>>();
        _mockPolicyFactory = new Mock<IPolicyFactory>();
        _mockFileTransferApiClient = new Mock<IFileTransferApiClient>();
        _mockStrategies = new Mock<IIndex<StorageProvider, IUploadStrategy>>();
        _mockSessionService = new Mock<ISessionService>();
        _mockAdaptiveController = new Mock<IAdaptiveUploadController>();
        _mockProcessor = new Mock<IFileUploadProcessor>();
        _mockSlicingManager = new Mock<IUploadSlicingManager>();

        _sharedFileDefinition = new SharedFileDefinition
        {
            Id = "id-1",
            SessionId = "session-1",
            UploadedFileLength = 123
        };

        var builder = new ContainerBuilder();
        // Register mocks as singletons for DI
        builder.RegisterInstance(_mockSlicerEncrypter.Object).As<ISlicerEncrypter>();
        builder.RegisterInstance(_mockCoordinatorLogger.Object).As<ILogger<FileUploadCoordinator>>();
        builder.RegisterInstance(_mockSlicerLogger.Object).As<ILogger<FileSlicer>>();
        builder.RegisterInstance(_mockWorkerLogger.Object).As<ILogger<FileUploadWorker>>();
        builder.RegisterInstance(_mockPolicyFactory.Object).As<IPolicyFactory>();
        builder.RegisterInstance(_mockFileTransferApiClient.Object).As<IFileTransferApiClient>();
        builder.RegisterInstance(_mockStrategies.Object).As<IIndex<StorageProvider, IUploadStrategy>>();
        builder.RegisterInstance(_mockSessionService.Object).As<ISessionService>();
        builder.RegisterInstance(_mockAdaptiveController.Object).As<IAdaptiveUploadController>();
        builder.RegisterInstance(_mockSlicingManager.Object).As<IUploadSlicingManager>();
        // Register the processor as an instance so typed parameters are ignored
        builder.RegisterInstance(_mockProcessor.Object).As<IFileUploadProcessor>();

        _container = builder.Build();
        _factory = new FileUploadProcessorFactory(_container);
    }

    [TearDown]
    public void TearDown()
    {
        _container.Dispose();
    }

    [Test]
    public void Create_Should_Return_Processor_From_DI()
    {
        // Act
        var result = _factory.Create(_mockSlicerEncrypter.Object, "C:/tmp/file.bin", null, _sharedFileDefinition);

        // Assert
        result.Should().BeSameAs(_mockProcessor.Object);
    }

    [Test]
    public void Create_With_MemoryStream_Should_Return_Processor()
    {
        // Arrange
        using var memory = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = _factory.Create(_mockSlicerEncrypter.Object, null, memory, _sharedFileDefinition);

        // Assert
        result.Should().BeSameAs(_mockProcessor.Object);
    }
}

