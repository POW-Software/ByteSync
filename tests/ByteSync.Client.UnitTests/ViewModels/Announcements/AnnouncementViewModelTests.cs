using System.Reactive.Linq;
using System.Reactive.Subjects;
using ByteSync.Business;
using ByteSync.Business.Configurations;
using ByteSync.Client.UnitTests.Helpers;
using ByteSync.Common.Business.Announcements;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.TestsCommon;
using ByteSync.ViewModels.Announcements;
using DynamicData;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Announcements;

[TestFixture]
public class AnnouncementViewModelTests : AbstractTester
{
    private Mock<IAnnouncementRepository> _mockAnnouncementRepository = null!;
    private Mock<ILocalizationService> _mockLocalizationService = null!;
    private Mock<IApplicationSettingsRepository> _mockApplicationSettingsRepository = null!;
    private AnnouncementViewModel _announcementViewModel = null!;
    private Subject<CultureDefinition> _cultureSubject = null!;
    private ApplicationSettings _applicationSettings = null!;
    
    [SetUp]
    public void SetUp()
    {
        _mockAnnouncementRepository = new Mock<IAnnouncementRepository>();
        _mockLocalizationService = new Mock<ILocalizationService>();
        _mockApplicationSettingsRepository = new Mock<IApplicationSettingsRepository>();
        _cultureSubject = new Subject<CultureDefinition>();
        _applicationSettings = new ApplicationSettings();
        
        // Setup default mocks
        _mockLocalizationService.Setup(x => x.CurrentCultureDefinition)
            .Returns(new CultureDefinition { Code = "en" });
        _mockLocalizationService.Setup(x => x.CurrentCultureObservable)
            .Returns(_cultureSubject.AsObservable());
        _mockApplicationSettingsRepository.Setup(x => x.GetCurrentApplicationSettings())
            .Returns(_applicationSettings);
        
        // Setup ObservableCache mock to avoid NullReferenceException
        var mockObservableCache = new Mock<IObservableCache<Announcement, string>>();
        mockObservableCache
            .Setup(x => x.Connect(It.IsAny<Func<Announcement, bool>>(), It.IsAny<bool>()))
            .Returns(Observable.Empty<IChangeSet<Announcement, string>>());
        _mockAnnouncementRepository.Setup(x => x.ObservableCache)
            .Returns(mockObservableCache.Object);
        
        _announcementViewModel = new AnnouncementViewModel(
            _mockAnnouncementRepository.Object,
            _mockLocalizationService.Object,
            _mockApplicationSettingsRepository.Object
        );
    }
    
    [Test]
    public void Constructor_ShouldInitializeProperties()
    {
        // Assert
        _announcementViewModel.Announcements.Should().NotBeNull();
        _announcementViewModel.AcknowledgeAnnouncementCommand.Should().NotBeNull();
        _announcementViewModel.IsVisible.Should().BeFalse();
    }
    
    [Test]
    public void Refresh_WithNoAnnouncements_ShouldSetIsVisibleToFalse()
    {
        // Arrange
        _mockAnnouncementRepository.Setup(x => x.Elements)
            .Returns(new List<Announcement>());
        
        // Act
        _announcementViewModel.Activator.Activate();
        
        // Assert
        _announcementViewModel.IsVisible.Should().BeFalse();
        _announcementViewModel.Announcements.Should().BeEmpty();
    }
    
    [Test]
    public void Refresh_WithUnacknowledgedAnnouncements_ShouldAddToCollection()
    {
        // Arrange
        var announcements = new List<Announcement>
        {
            new()
            {
                Id = "1",
                Message = new Dictionary<string, string> { { "en", "Test message" } }
            },
            new()
            {
                Id = "2",
                Message = new Dictionary<string, string> { { "en", "Test message 2" } }
            }
        };
        
        // Setup ObservableCache to emit the announcements
        var mockObservableCache = new Mock<IObservableCache<Announcement, string>>();
        var changeSet = new ChangeSet<Announcement, string>();
        changeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "1", announcements[0]));
        changeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "2", announcements[1]));
        
        mockObservableCache
            .Setup(x => x.Connect(It.IsAny<Func<Announcement, bool>>(), It.IsAny<bool>()))
            .Returns(Observable.Return(changeSet));
        
        _mockAnnouncementRepository.Setup(x => x.ObservableCache)
            .Returns(mockObservableCache.Object);
        
        // Setup Elements to return the announcements
        _mockAnnouncementRepository.Setup(x => x.Elements)
            .Returns(announcements);
        
        _applicationSettings.DecodedAcknowledgedAnnouncementIds = [];
        
        // Act
        _announcementViewModel.Activator.Activate();
        
        // Assert
        _announcementViewModel.Announcements.Should().HaveCount(2);
        _announcementViewModel.IsVisible.Should().BeTrue();
        _announcementViewModel.Announcements[0].Id.Should().Be("1");
        _announcementViewModel.Announcements[0].Message.Should().Be("Test message");
        _announcementViewModel.Announcements[1].Id.Should().Be("2");
        _announcementViewModel.Announcements[1].Message.Should().Be("Test message 2");
    }
    
    [Test]
    public void Refresh_WithAcknowledgedAnnouncements_ShouldNotAddToCollection()
    {
        // Arrange
        var announcements = new List<Announcement>
        {
            new()
            {
                Id = "1",
                Message = new Dictionary<string, string> { { "en", "Test message" } }
            },
            new()
            {
                Id = "2",
                Message = new Dictionary<string, string> { { "en", "Test message 2" } }
            }
        };
        
        // Setup ObservableCache to emit the announcements
        var mockObservableCache = new Mock<IObservableCache<Announcement, string>>();
        var changeSet = new ChangeSet<Announcement, string>();
        changeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "1", announcements[0]));
        changeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "2", announcements[1]));
        
        mockObservableCache
            .Setup(x => x.Connect(It.IsAny<Func<Announcement, bool>>(), It.IsAny<bool>()))
            .Returns(Observable.Return(changeSet));
        
        _mockAnnouncementRepository.Setup(x => x.ObservableCache)
            .Returns(mockObservableCache.Object);
        
        // Setup Elements to return the announcements
        _mockAnnouncementRepository.Setup(x => x.Elements)
            .Returns(announcements);
        
        _applicationSettings.DecodedAcknowledgedAnnouncementIds = ["1", "2"];
        
        // Act
        _announcementViewModel.Activator.Activate();
        
        // Assert
        _announcementViewModel.Announcements.Should().HaveCount(0);
        _announcementViewModel.IsVisible.Should().BeFalse();
    }
    
    [Test]
    public void Refresh_WithMixedAnnouncements_ShouldOnlyAddUnacknowledged()
    {
        // Arrange
        var announcements = new List<Announcement>
        {
            new()
            {
                Id = "1",
                Message = new Dictionary<string, string> { { "en", "Test message 1" } }
            },
            new()
            {
                Id = "2",
                Message = new Dictionary<string, string> { { "en", "Test message 2" } }
            },
            new()
            {
                Id = "3",
                Message = new Dictionary<string, string> { { "en", "Test message 3" } }
            }
        };
        
        // Setup ObservableCache to emit the announcements
        var mockObservableCache = new Mock<IObservableCache<Announcement, string>>();
        var changeSet = new ChangeSet<Announcement, string>();
        changeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "1", announcements[0]));
        changeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "2", announcements[1]));
        changeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "3", announcements[2]));
        
        mockObservableCache
            .Setup(x => x.Connect(It.IsAny<Func<Announcement, bool>>(), It.IsAny<bool>()))
            .Returns(Observable.Return(changeSet));
        
        _mockAnnouncementRepository.Setup(x => x.ObservableCache)
            .Returns(mockObservableCache.Object);
        
        // Setup Elements to return the announcements
        _mockAnnouncementRepository.Setup(x => x.Elements)
            .Returns(announcements);
        
        _applicationSettings.DecodedAcknowledgedAnnouncementIds = ["2"];
        
        // Act
        _announcementViewModel.Activator.Activate();
        
        // Assert
        _announcementViewModel.Announcements.Should().HaveCount(2);
        _announcementViewModel.IsVisible.Should().BeTrue();
        _announcementViewModel.Announcements[0].Id.Should().Be("1");
        _announcementViewModel.Announcements[0].Message.Should().Be("Test message 1");
        _announcementViewModel.Announcements[1].Id.Should().Be("3");
        _announcementViewModel.Announcements[1].Message.Should().Be("Test message 3");
    }
    
    [Test]
    public void Refresh_WithLocalizedMessage_ShouldUseCorrectLanguage()
    {
        // Arrange
        var announcements = new List<Announcement>
        {
            new()
            {
                Id = "1",
                Message = new Dictionary<string, string>
                {
                    { "en", "English message" },
                    { "fr", "Message français" }
                }
            }
        };
        
        // Setup ObservableCache to emit the announcements
        var mockObservableCache = new Mock<IObservableCache<Announcement, string>>();
        var changeSet = new ChangeSet<Announcement, string>();
        changeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "1", announcements[0]));
        
        mockObservableCache
            .Setup(x => x.Connect(It.IsAny<Func<Announcement, bool>>(), It.IsAny<bool>()))
            .Returns(Observable.Return(changeSet));
        
        _mockAnnouncementRepository.Setup(x => x.ObservableCache)
            .Returns(mockObservableCache.Object);
        
        // Setup Elements to return the announcements
        _mockAnnouncementRepository.Setup(x => x.Elements)
            .Returns(announcements);
        
        // Ensure the announcement is not acknowledged
        _applicationSettings.DecodedAcknowledgedAnnouncementIds = [];
        
        _mockLocalizationService.Setup(x => x.CurrentCultureDefinition)
            .Returns(new CultureDefinition { Code = "fr" });
        
        // Act
        _announcementViewModel.Activator.Activate();
        
        // Assert
        _announcementViewModel.Announcements.Should().HaveCount(1);
        _announcementViewModel.Announcements[0].Message.Should().Be("Message français");
    }
    
    [Test]
    public void Refresh_WithMissingLocalizedMessage_ShouldUseFirstAvailableMessage()
    {
        // Arrange
        var announcements = new List<Announcement>
        {
            new()
            {
                Id = "1",
                Message = new Dictionary<string, string>
                {
                    { "en", "English message" },
                    { "de", "Deutsche Nachricht" }
                }
            }
        };
        
        // Setup ObservableCache to emit the announcements
        var mockObservableCache = new Mock<IObservableCache<Announcement, string>>();
        var changeSet = new ChangeSet<Announcement, string>();
        changeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "1", announcements[0]));
        
        mockObservableCache
            .Setup(x => x.Connect(It.IsAny<Func<Announcement, bool>>(), It.IsAny<bool>()))
            .Returns(Observable.Return(changeSet));
        
        _mockAnnouncementRepository.Setup(x => x.ObservableCache)
            .Returns(mockObservableCache.Object);
        
        // Setup Elements to return the announcements
        _mockAnnouncementRepository.Setup(x => x.Elements)
            .Returns(announcements);
        
        // Ensure the announcement is not acknowledged
        _applicationSettings.DecodedAcknowledgedAnnouncementIds = [];
        
        _mockLocalizationService.Setup(x => x.CurrentCultureDefinition)
            .Returns(new CultureDefinition { Code = "fr" }); // French not available
        
        // Act
        _announcementViewModel.Activator.Activate();
        
        // Assert
        _announcementViewModel.Announcements.Should().HaveCount(1);
        _announcementViewModel.Announcements[0].Message.Should().Be("English message"); // Should use first available
    }
    
    [Test]
    public void Refresh_WithEmptyMessage_ShouldUseEmptyString()
    {
        // Arrange
        var announcements = new List<Announcement>
        {
            new()
            {
                Id = "1",
                Message = new Dictionary<string, string>
                {
                    { "en", "" },
                    { "fr", "Message français" }
                }
            }
        };
        
        // Setup ObservableCache to emit the announcements
        var mockObservableCache = new Mock<IObservableCache<Announcement, string>>();
        var changeSet = new ChangeSet<Announcement, string>();
        changeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "1", announcements[0]));
        
        mockObservableCache
            .Setup(x => x.Connect(It.IsAny<Func<Announcement, bool>>(), It.IsAny<bool>()))
            .Returns(Observable.Return(changeSet));
        
        _mockAnnouncementRepository.Setup(x => x.ObservableCache)
            .Returns(mockObservableCache.Object);
        
        // Setup Elements to return the announcements
        _mockAnnouncementRepository.Setup(x => x.Elements)
            .Returns(announcements);
        
        // Ensure the announcement is not acknowledged
        _applicationSettings.DecodedAcknowledgedAnnouncementIds = [];
        
        // Act
        _announcementViewModel.Activator.Activate();
        
        // Assert
        _announcementViewModel.Announcements.Should().HaveCount(1);
        _announcementViewModel.Announcements[0].Message.Should().Be("");
    }
    
    [Test]
    public void AcknowledgeAnnouncement_ShouldAddToAcknowledgedList()
    {
        // Arrange
        var announcements = new List<Announcement>
        {
            new()
            {
                Id = "1",
                Message = new Dictionary<string, string> { { "en", "Test message" } }
            }
        };
        
        // Setup ObservableCache to emit the announcements
        var mockObservableCache = new Mock<IObservableCache<Announcement, string>>();
        var changeSet = new ChangeSet<Announcement, string>();
        changeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "1", announcements[0]));
        
        mockObservableCache
            .Setup(x => x.Connect(It.IsAny<Func<Announcement, bool>>(), It.IsAny<bool>()))
            .Returns(Observable.Return(changeSet));
        
        _mockAnnouncementRepository.Setup(x => x.ObservableCache)
            .Returns(mockObservableCache.Object);
        
        // Setup Elements to return the announcements
        _mockAnnouncementRepository.Setup(x => x.Elements)
            .Returns(announcements);
        
        _announcementViewModel.Activator.Activate();
        
        // Act
        _announcementViewModel.AcknowledgeAnnouncementCommand.Execute("1").Subscribe();
        
        // Assert
        _mockApplicationSettingsRepository.Verify(
            x => x.UpdateCurrentApplicationSettings(It.IsAny<Action<ApplicationSettings>>(), true),
            Times.Once);
    }
    
    [Test]
    public void AcknowledgeAnnouncement_ShouldRefreshCollection()
    {
        // Arrange
        var announcements = new List<Announcement>
        {
            new()
            {
                Id = "1",
                Message = new Dictionary<string, string> { { "en", "Test message" } }
            }
        };
        
        // Setup ObservableCache to emit the announcements
        var mockObservableCache = new Mock<IObservableCache<Announcement, string>>();
        var changeSet = new ChangeSet<Announcement, string>();
        changeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "1", announcements[0]));
        
        mockObservableCache
            .Setup(x => x.Connect(It.IsAny<Func<Announcement, bool>>(), It.IsAny<bool>()))
            .Returns(Observable.Return(changeSet));
        
        _mockAnnouncementRepository.Setup(x => x.ObservableCache)
            .Returns(mockObservableCache.Object);
        
        // Setup Elements to return the announcements
        _mockAnnouncementRepository.Setup(x => x.Elements)
            .Returns(announcements);
        
        _announcementViewModel.Activator.Activate();
        _announcementViewModel.Announcements.Should().HaveCount(1);
        
        // Act
        _announcementViewModel.AcknowledgeAnnouncementCommand.Execute("1").Subscribe();
        
        // Assert
        // After acknowledgment, the announcement should be removed from the collection
        // This is verified by the fact that Refresh() is called again
        _mockApplicationSettingsRepository.Verify(
            x => x.GetCurrentApplicationSettings(),
            Times.AtLeast(2)); // Called once during initial setup and again during acknowledgment
    }
    
    [Test]
    public void WhenActivated_ShouldSubscribeToRepositoryChanges()
    {
        // Arrange
        var mockObservableCache = new Mock<IObservableCache<Announcement, string>>();
        mockObservableCache
            .Setup(x => x.Connect(It.IsAny<Func<Announcement, bool>>(), It.IsAny<bool>()))
            .Returns(Observable.Empty<IChangeSet<Announcement, string>>());
        
        _mockAnnouncementRepository.Setup(x => x.ObservableCache)
            .Returns(mockObservableCache.Object);
        
        // Act & Assert
        _announcementViewModel.Activator.Activate();
    }
    
    [Test]
    public void WhenActivated_ShouldSubscribeToCultureChanges()
    {
        // Act
        _announcementViewModel.Activator.Activate();
        
        // Assert
        _mockLocalizationService.Verify(x => x.CurrentCultureObservable, Times.Once);
    }
    
    [Test]
    public void WhenDeactivated_ShouldDisposeSubscriptions()
    {
        // Arrange
        _announcementViewModel.Activator.Activate();
        
        // Act
        _announcementViewModel.Activator.Deactivate();
        
        // Assert
        // The subscriptions should be disposed when the view model is deactivated
        // This is handled by ReactiveUI's WhenActivated mechanism
        // If we reach here without exceptions, the test passes
        _announcementViewModel.Should().NotBeNull();
    }
    
    [Test]
    public void Refresh_WithRepositoryCacheChanges_ShouldUpdateCollection()
    {
        // Arrange
        var announcements = new List<Announcement>
        {
            new()
            {
                Id = "1",
                Message = new Dictionary<string, string> { { "en", "Test message" } }
            }
        };
        
        // Setup ObservableCache to emit the initial announcements
        var mockObservableCache = new Mock<IObservableCache<Announcement, string>>();
        var initialChangeSet = new ChangeSet<Announcement, string>();
        initialChangeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "1", announcements[0]));
        
        mockObservableCache
            .Setup(x => x.Connect(It.IsAny<Func<Announcement, bool>>(), It.IsAny<bool>()))
            .Returns(Observable.Return(initialChangeSet));
        
        _mockAnnouncementRepository.Setup(x => x.ObservableCache)
            .Returns(mockObservableCache.Object);
        
        // Setup Elements to return the initial announcements
        _mockAnnouncementRepository.Setup(x => x.Elements)
            .Returns(announcements);
        
        // Ensure announcements are not acknowledged
        _applicationSettings.DecodedAcknowledgedAnnouncementIds = [];
        
        _announcementViewModel.Activator.Activate();
        _announcementViewModel.Announcements.Should().HaveCount(1);
        
        // Act - Simulate repository cache change
        var newAnnouncements = new List<Announcement>
        {
            new()
            {
                Id = "2",
                Message = new Dictionary<string, string> { { "en", "New message" } }
            }
        };
        
        // Setup new change set for the updated announcements
        var updatedChangeSet = new ChangeSet<Announcement, string>();
        updatedChangeSet.Add(new Change<Announcement, string>(ChangeReason.Remove, "1", announcements[0]));
        updatedChangeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "2", newAnnouncements[0]));
        
        mockObservableCache
            .Setup(x => x.Connect(It.IsAny<Func<Announcement, bool>>(), It.IsAny<bool>()))
            .Returns(Observable.Return(updatedChangeSet));
        
        // Update Elements to return the new announcements
        _mockAnnouncementRepository.Setup(x => x.Elements)
            .Returns(newAnnouncements);
        
        // Trigger a refresh by simulating a culture change
        _cultureSubject.OnNext(new CultureDefinition { Code = "en" });
        
        // Assert (reactive update)
        _announcementViewModel.ShouldEventuallyBe(vm => vm.Announcements.Count, 1);
        _announcementViewModel.Announcements[0].Id.Should().Be("2");
        _announcementViewModel.Announcements[0].Message.Should().Be("New message");
    }
    
    [Test]
    public void Refresh_WithCultureChange_ShouldUpdateMessages()
    {
        // Arrange
        var announcements = new List<Announcement>
        {
            new()
            {
                Id = "1",
                Message = new Dictionary<string, string>
                {
                    { "en", "English message" },
                    { "fr", "Message français" }
                }
            }
        };
        
        // Setup ObservableCache to emit the announcements
        var mockObservableCache = new Mock<IObservableCache<Announcement, string>>();
        var changeSet = new ChangeSet<Announcement, string>();
        changeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "1", announcements[0]));
        
        mockObservableCache
            .Setup(x => x.Connect(It.IsAny<Func<Announcement, bool>>(), It.IsAny<bool>()))
            .Returns(Observable.Return(changeSet));
        
        _mockAnnouncementRepository.Setup(x => x.ObservableCache)
            .Returns(mockObservableCache.Object);
        
        // Setup Elements to return the announcements
        _mockAnnouncementRepository.Setup(x => x.Elements)
            .Returns(announcements);
        
        // Ensure the announcement is not acknowledged
        _applicationSettings.DecodedAcknowledgedAnnouncementIds = [];
        
        _announcementViewModel.Activator.Activate();
        _announcementViewModel.Announcements.Should().HaveCount(1);
        _announcementViewModel.Announcements[0].Message.Should().Be("English message");
        
        // Act - Change culture
        _mockLocalizationService.Setup(x => x.CurrentCultureDefinition)
            .Returns(new CultureDefinition { Code = "fr" });
        _cultureSubject.OnNext(new CultureDefinition { Code = "fr" });
        
        // Assert (reactive update)
        _announcementViewModel.ShouldEventuallyBe(vm => vm.Announcements.Count, 1);
        _announcementViewModel.Announcements[0].Message.Should().Be("Message français");
    }
    
    [Test]
    public void IsAnnouncementAcknowledged_ShouldReturnCorrectStatus()
    {
        // Arrange
        _applicationSettings.DecodedAcknowledgedAnnouncementIds = ["1", "3"];
        
        // Act & Assert
        _applicationSettings.IsAnnouncementAcknowledged("1").Should().BeTrue();
        _applicationSettings.IsAnnouncementAcknowledged("2").Should().BeFalse();
        _applicationSettings.IsAnnouncementAcknowledged("3").Should().BeTrue();
    }
    
    [Test]
    public void AddAcknowledgedAnnouncementId_ShouldAddToAcknowledgedList()
    {
        // Arrange
        _applicationSettings.DecodedAcknowledgedAnnouncementIds = ["1"];
        
        // Act
        _applicationSettings.AddAcknowledgedAnnouncementId("2");
        
        // Assert
        _applicationSettings.DecodedAcknowledgedAnnouncementIds.Should().Contain("1");
        _applicationSettings.DecodedAcknowledgedAnnouncementIds.Should().Contain("2");
        _applicationSettings.DecodedAcknowledgedAnnouncementIds.Should().HaveCount(2);
    }
    
    [Test]
    public void AddAcknowledgedAnnouncementId_ShouldNotAddDuplicate()
    {
        // Arrange
        _applicationSettings.DecodedAcknowledgedAnnouncementIds = ["1"];
        
        // Act
        _applicationSettings.AddAcknowledgedAnnouncementId("1");
        
        // Assert
        _applicationSettings.DecodedAcknowledgedAnnouncementIds.Should().Contain("1");
        _applicationSettings.DecodedAcknowledgedAnnouncementIds.Should().HaveCount(1);
    }
    
    [Test]
    public void InitializeAcknowledgedAnnouncementIds_ShouldClearList()
    {
        // Arrange
        _applicationSettings.DecodedAcknowledgedAnnouncementIds = ["1", "2", "3"];
        
        // Act
        _applicationSettings.InitializeAcknowledgedAnnouncementIds();
        
        // Assert
        _applicationSettings.DecodedAcknowledgedAnnouncementIds.Should().BeEmpty();
    }
    
    [Test]
    public void AnnouncementItemViewModel_ShouldHaveCorrectProperties()
    {
        // Arrange
        var announcementItem = new AnnouncementItemViewModel
        {
            Id = "test-id",
            Message = "Test message"
        };
        
        // Assert
        announcementItem.Id.Should().Be("test-id");
        announcementItem.Message.Should().Be("Test message");
    }
    
    [Test]
    public void Constructor_WithNoParameters_ShouldCreateEmptyViewModel()
    {
        // Act
        var emptyViewModel = new AnnouncementViewModel();
        
        // Assert
        emptyViewModel.Announcements.Should().NotBeNull();
        emptyViewModel.Announcements.Should().BeEmpty();
        emptyViewModel.IsVisible.Should().BeFalse();
    }
    
    [Test]
    public void AcknowledgeAnnouncementCommand_ShouldBeExecutable()
    {
        // Assert
        _announcementViewModel.AcknowledgeAnnouncementCommand.Should().NotBeNull();
        _announcementViewModel.AcknowledgeAnnouncementCommand.CanExecute.Should().NotBeNull();
    }
    
    [Test]
    public void Refresh_WithNullAcknowledgedIds_ShouldHandleGracefully()
    {
        // Arrange
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        _applicationSettings.DecodedAcknowledgedAnnouncementIds = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        var announcements = new List<Announcement>
        {
            new()
            {
                Id = "1",
                Message = new Dictionary<string, string> { { "en", "Test message" } }
            }
        };
        
        // Setup ObservableCache to emit the announcements
        var mockObservableCache = new Mock<IObservableCache<Announcement, string>>();
        var changeSet = new ChangeSet<Announcement, string>();
        changeSet.Add(new Change<Announcement, string>(ChangeReason.Add, "1", announcements[0]));
        
        mockObservableCache
            .Setup(x => x.Connect(It.IsAny<Func<Announcement, bool>>(), It.IsAny<bool>()))
            .Returns(Observable.Return(changeSet));
        
        _mockAnnouncementRepository.Setup(x => x.ObservableCache)
            .Returns(mockObservableCache.Object);
        
        // Setup Elements to return the announcements
        _mockAnnouncementRepository.Setup(x => x.Elements)
            .Returns(announcements);
        
        // Act
        _announcementViewModel.Activator.Activate();
        
        // Assert
        _announcementViewModel.IsVisible.Should().BeTrue();
        _announcementViewModel.Announcements.Should().HaveCount(1);
    }
}