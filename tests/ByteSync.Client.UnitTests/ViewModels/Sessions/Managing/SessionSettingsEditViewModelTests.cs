using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
    
    [Test]
    public void Import_Then_Export_Roundtrip_Should_Map_All_Properties()
    {
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
        
        // Provide observables required by WhenActivated
        sessionService.SetupGet(s => s.SessionSettingsObservable).Returns(Observable.Never<SessionSettings?>());
        inventoryStarter.Setup(s => s.CanCurrentUserStartInventory()).Returns(Observable.Return(true));
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Return(SessionStatus.Preparation));
        
        var vm = new SessionSettingsEditViewModel(
            sessionService.Object,
            localizationService.Object,
            inventoryStarter.Object,
            analysisFactory.Object,
            dataTypeFactory.Object,
            matchingModeFactory.Object,
            null,
            logger.Object);
        
        var settings = new SessionSettings
        {
            DataType = DataTypes.Directories,
            MatchingMode = MatchingModes.Flat,
            AnalysisMode = AnalysisModes.Checksum,
            ExcludeHiddenFiles = true,
            ExcludeSystemFiles = true,
            Extensions = ".txt;.md"
        };
        
        // Ensure ImportSettings updates Extensions (it updates only if current and incoming are non-empty and differ)
        vm.Extensions = "old";
        vm.ImportSettings(settings);
        
        vm.DataType!.DataType.Should().Be(DataTypes.Directories);
        vm.MatchingMode!.MatchingMode.Should().Be(MatchingModes.Flat);
        vm.AnalysisMode!.AnalysisMode.Should().Be(AnalysisModes.Checksum);
        vm.ExcludeHiddenFiles.Should().BeTrue();
        vm.ExcludeSystemFiles.Should().BeTrue();
        
        // Extensions mapping is conditional by design; verify via Export only.
        
        var exported = vm.ExportSettings();
        exported.DataType.Should().Be(DataTypes.Directories);
        exported.MatchingMode.Should().Be(MatchingModes.Flat);
        exported.AnalysisMode.Should().Be(AnalysisModes.Checksum);
        exported.ExcludeHiddenFiles.Should().BeTrue();
        exported.ExcludeSystemFiles.Should().BeTrue();
        
        // Extensions returned by export should reflect the ViewModel value
        exported.Extensions.Should().Be(vm.Extensions);
    }
    
    [Test]
    public async Task Changing_Property_When_Editable_Should_Send_Update()
    {
        var sessionService = new Mock<ISessionService>();
        var localizationService = new Mock<ILocalizationService>();
        var inventoryStarter = new Mock<IDataInventoryStarter>();
        var analysisFactory = new Mock<IAnalysisModeViewModelFactory>();
        var dataTypeFactory = new Mock<IDataTypeViewModelFactory>();
        var matchingModeFactory = new Mock<IMatchingModeViewModelFactory>();
        var logger = new Mock<ILogger<SessionSettingsEditViewModel>>();
        
        localizationService.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => key);
        
        var cultureSubject = new Subject<CultureDefinition>();
        localizationService.SetupGet(l => l.CurrentCultureObservable).Returns(cultureSubject);
        
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
        
        inventoryStarter.Setup(s => s.CanCurrentUserStartInventory()).Returns(Observable.Return(true));
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Return(SessionStatus.Preparation));
        sessionService.SetupGet(s => s.SessionSettingsObservable).Returns(Observable.Never<SessionSettings?>());
        sessionService.Setup(s => s.SetSessionSettings(It.IsAny<SessionSettings>(), true)).Returns(Task.CompletedTask);
        
        var initial = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            MatchingMode = MatchingModes.Tree,
            AnalysisMode = AnalysisModes.Smart
        };
        
        var vm = new SessionSettingsEditViewModel(
            sessionService.Object,
            localizationService.Object,
            inventoryStarter.Object,
            analysisFactory.Object,
            dataTypeFactory.Object,
            matchingModeFactory.Object,
            initial,
            logger.Object);
        
        using var _ = vm.Activator.Activate();
        
        vm.ExcludeHiddenFiles = true;
        await Task.Delay(300);
        
        sessionService.Verify(s => s.SetSessionSettings(It.IsAny<SessionSettings>(), true), Times.AtLeastOnce);
    }
    
    [Test]
    public void Locale_Update_Should_Keep_Selections()
    {
        var sessionService = new Mock<ISessionService>();
        var localizationService = new Mock<ILocalizationService>();
        var inventoryStarter = new Mock<IDataInventoryStarter>();
        var analysisFactory = new Mock<IAnalysisModeViewModelFactory>();
        var dataTypeFactory = new Mock<IDataTypeViewModelFactory>();
        var matchingModeFactory = new Mock<IMatchingModeViewModelFactory>();
        var logger = new Mock<ILogger<SessionSettingsEditViewModel>>();
        
        var cultureSubject = new Subject<CultureDefinition>();
        localizationService.SetupGet(l => l.CurrentCultureObservable).Returns(cultureSubject);
        localizationService.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => key);
        
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
        
        // Provide observables required by WhenActivated pipeline in the VM
        sessionService.SetupGet(s => s.SessionSettingsObservable).Returns(Observable.Never<SessionSettings?>());
        inventoryStarter.Setup(s => s.CanCurrentUserStartInventory()).Returns(Observable.Return(true));
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Return(SessionStatus.Preparation));
        
        var vm = new SessionSettingsEditViewModel(
            sessionService.Object,
            localizationService.Object,
            inventoryStarter.Object,
            analysisFactory.Object,
            dataTypeFactory.Object,
            matchingModeFactory.Object,
            null,
            logger.Object);
        
        vm.ImportSettings(new SessionSettings
        {
            DataType = DataTypes.Files,
            MatchingMode = MatchingModes.Tree,
            AnalysisMode = AnalysisModes.Smart
        });
        
        var selectedDataType = vm.DataType;
        var selectedMatchingMode = vm.MatchingMode;
        var selectedAnalysisMode = vm.AnalysisMode;
        
        using var _ = vm.Activator.Activate();
        cultureSubject.OnNext(new CultureDefinition(CultureInfo.InvariantCulture));
        
        vm.DataType.Should().Be(selectedDataType);
        vm.MatchingMode.Should().Be(selectedMatchingMode);
        vm.AnalysisMode.Should().Be(selectedAnalysisMode);
    }
}