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
        var linkingKeyFactory = new Mock<ILinkingKeyViewModelFactory>();
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
        
        linkingKeyFactory.Setup(f => f.CreateLinkingKeyViewModel(LinkingKeys.RelativePath))
            .Returns(new LinkingKeyViewModel(LinkingKeys.RelativePath, localizationService.Object));
        linkingKeyFactory.Setup(f => f.CreateLinkingKeyViewModel(LinkingKeys.Name))
            .Returns(new LinkingKeyViewModel(LinkingKeys.Name, localizationService.Object));
        
        var initialSettings = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            LinkingKey = LinkingKeys.Name,
            AnalysisMode = AnalysisModes.Smart
        };
        
        // Act
        var vm = new SessionSettingsEditViewModel(
            sessionService.Object,
            localizationService.Object,
            inventoryStarter.Object,
            analysisFactory.Object,
            dataTypeFactory.Object,
            linkingKeyFactory.Object,
            initialSettings,
            logger.Object);
        
        // Assert
        vm.Should().NotBeNull();
        vm.AvailableAnalysisModes.Should().HaveCount(2);
        vm.AvailableDataTypes.Should().HaveCount(3);
        vm.AvailableLinkingKeys.Should().HaveCount(2);
    }
}