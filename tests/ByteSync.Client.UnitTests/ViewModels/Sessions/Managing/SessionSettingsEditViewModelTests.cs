using System.Reactive.Linq;
using ByteSync.Business;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.ViewModels.Sessions.Managing;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Sessions.Managing;

[TestFixture]
public class SessionSettingsEditViewModelTests
{
    [Test]
    public void Constructor_WithAllDependencies_ShouldCreateInstance()
    {
        // Arrange
        var sessionService = new Mock<ISessionService>();
        var localizationService = new Mock<ILocalizationService>();
        var inventoryStarter = new Mock<IDataInventoryStarter>();
        var analysisFactory = new Mock<IAnalysisModeViewModelFactory>();
        var dataTypeFactory = new Mock<IDataTypeViewModelFactory>();
        var matchingModeFactory = new Mock<IMatchingModeViewModelFactory>();
        var logger = new Mock<ILogger<SessionSettingsEditViewModel>>();
        
        localizationService.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => key);
        localizationService.SetupGet(l => l.CurrentCultureObservable)
            .Returns(Observable.Never<CultureDefinition>());
        
        analysisFactory.Setup(f => f.CreateAnalysisModeViewModel(AnalysisModes.Smart))
            .Returns(new AnalysisModeViewModel(localizationService.Object, AnalysisModes.Smart));
        analysisFactory.Setup(f => f.CreateAnalysisModeViewModel(AnalysisModes.Checksum))
            .Returns(new AnalysisModeViewModel(localizationService.Object, AnalysisModes.Checksum));
        
        dataTypeFactory.Setup(f => f.CreateDataTypeViewModel(DataTypes.FilesDirectories))
            .Returns(new DataTypeViewModel(DataTypes.FilesDirectories, localizationService.Object));
        dataTypeFactory.Setup(f => f.CreateDataTypeViewModel(DataTypes.Files))
            .Returns(new DataTypeViewModel(DataTypes.Files, localizationService.Object));
        dataTypeFactory.Setup(f => f.CreateDataTypeViewModel(DataTypes.Directories))
            .Returns(new DataTypeViewModel(DataTypes.Directories, localizationService.Object));
        
        matchingModeFactory.Setup(f => f.CreateMatchingModeViewModel(MatchingModes.Tree))
            .Returns(new MatchingModeViewModel(MatchingModes.Tree, localizationService.Object));
        matchingModeFactory.Setup(f => f.CreateMatchingModeViewModel(MatchingModes.Flat))
            .Returns(new MatchingModeViewModel(MatchingModes.Flat, localizationService.Object));
        
        var initialSettings = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            MatchingMode = MatchingModes.Flat,
            AnalysisMode = AnalysisModes.Smart
        };
        
        // Act
        var vm = new SessionSettingsEditViewModel(
            sessionService.Object,
            localizationService.Object,
            inventoryStarter.Object,
            analysisFactory.Object,
            dataTypeFactory.Object,
            matchingModeFactory.Object,
            initialSettings,
            logger.Object);
        
        // Assert
        vm.Should().NotBeNull();
        vm.AvailableAnalysisModes.Should().HaveCount(2);
        vm.AvailableDataTypes.Should().HaveCount(3);
        vm.AvailableMatchingModes.Should().HaveCount(2);
    }
}