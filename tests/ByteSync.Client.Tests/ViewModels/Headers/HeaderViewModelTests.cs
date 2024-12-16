using System.Reactive.Subjects;
using ByteSync.Business;
using ByteSync.Business.Navigations;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Updates;
using ByteSync.ViewModels.Headers;
using DynamicData;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace ByteSync.Tests.ViewModels.Headers;

[TestFixture]
public class HeaderViewModelTests
{
    private readonly Mock<IWebAccessor> _webAccessorMock;
    private readonly Mock<IAvailableUpdateRepository> _updateServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    
    public HeaderViewModelTests()
    {
        _webAccessorMock = new Mock<IWebAccessor>();
        _updateServiceMock = new Mock<IAvailableUpdateRepository>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _navigationServiceMock = new Mock<INavigationService>();
    }

    [Test]
    public void When_Activated_Should_SubscribeToNextVersions_Mandatory()
    {
        // Arrange
        var testSoftwareVersion = new SoftwareVersion 
        { 
            Level = PriorityLevel.Minimal,
            Version = "1.0.0"
        };

        var sourceCache = new SourceCache<SoftwareVersion, string>(sv => sv.Version);
        var observable = sourceCache.Connect().AsObservableCache();

        _navigationServiceMock
            .Setup(s => s.CurrentPanel)
            .Returns(new Subject<NavigationDetails>());
        
        _localizationServiceMock
            .Setup(s => s.CurrentCultureObservable)
            .Returns(new Subject<CultureDefinition>());
        
        _updateServiceMock
            .Setup(s => s.ObservableCache)
            .Returns(observable);
        
        sourceCache.AddOrUpdate(testSoftwareVersion);

        var headerViewModel = new HeaderViewModel(null, null, null, _webAccessorMock.Object, _updateServiceMock.Object, 
            _localizationServiceMock.Object, _navigationServiceMock.Object);

        // Act
        // Normally you would need to simulate the activation of the ViewModel, but since the WhenActivated logic is inside the constructor, it gets activated immediately.
        headerViewModel.Activator.Activate();

        // Assert
        ClassicAssert.True(headerViewModel.IsAVersionMandatory);
        ClassicAssert.True(headerViewModel.ShowUpdateObservable);
    }
    
    [Test]
    public void When_Activated_Should_SubscribeToNextVersions_NonMandatory()
    {
        // Arrange
        var testSoftwareVersion = new SoftwareVersion 
        { 
            Level = PriorityLevel.Recommended,
            Version = "1.0.0"
        };

        var sourceCache = new SourceCache<SoftwareVersion, string>(sv => sv.Version);
        var observable = sourceCache.Connect().AsObservableCache();

        _navigationServiceMock
            .Setup(s => s.CurrentPanel)
            .Returns(new Subject<NavigationDetails>());
        
        _localizationServiceMock
            .Setup(s => s.CurrentCultureObservable)
            .Returns(new Subject<CultureDefinition>());
        
        _updateServiceMock
            .Setup(s => s.ObservableCache)
            .Returns(observable);
        
        sourceCache.AddOrUpdate(testSoftwareVersion);

        var headerViewModel = new HeaderViewModel(null, null, null, _webAccessorMock.Object, _updateServiceMock.Object, 
            _localizationServiceMock.Object, _navigationServiceMock.Object);

        // Act
        // Normally you would need to simulate the activation of the ViewModel, but since the WhenActivated logic is inside the constructor, it gets activated immediately.
        headerViewModel.Activator.Activate();

        // Assert
        ClassicAssert.False(headerViewModel.IsAVersionMandatory);
        ClassicAssert.True(headerViewModel.ShowUpdateObservable);
    }
    
    [Test]
    public void When_Activated_Should_SubscribeToNextVersions_MandatoryThenEmpty()
    {
        // Arrange
        var testSoftwareVersion = new SoftwareVersion 
        { 
            Level = PriorityLevel.Minimal,
            Version = "1.0.0"
        };

        var sourceCache = new SourceCache<SoftwareVersion, string>(sv => sv.Version);
        var observable = sourceCache.Connect().AsObservableCache();

        _navigationServiceMock
            .Setup(s => s.CurrentPanel)
            .Returns(new Subject<NavigationDetails>());
        
        _localizationServiceMock
            .Setup(s => s.CurrentCultureObservable)
            .Returns(new Subject<CultureDefinition>());
        
        _updateServiceMock
            .Setup(s => s.ObservableCache)
            .Returns(observable);
        
        sourceCache.AddOrUpdate(testSoftwareVersion);

        var headerViewModel = new HeaderViewModel(null, null, null, _webAccessorMock.Object, _updateServiceMock.Object, 
            _localizationServiceMock.Object, _navigationServiceMock.Object);

        // Act
        // Normally you would need to simulate the activation of the ViewModel, but since the WhenActivated logic is inside the constructor, it gets activated immediately.
        headerViewModel.Activator.Activate();

        // Assert
        ClassicAssert.True(headerViewModel.IsAVersionMandatory);
        ClassicAssert.True(headerViewModel.ShowUpdateObservable);
        
        // POST : Empty
        sourceCache.Remove(testSoftwareVersion);
        // Assert
        ClassicAssert.False(headerViewModel.IsAVersionMandatory);
        ClassicAssert.False(headerViewModel.ShowUpdateObservable);
        
    }
}