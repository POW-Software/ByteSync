using Autofac;
using ByteSync.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Client.IntegrationTests.TestHelpers.Business;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Services.Sessions;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.Models.Comparisons;

public class TestInventoryComparer : IntegrationTest
{
    [SetUp]
    public void SetUp()
    {
        RegisterType<CloudSessionLocalDataManager, ICloudSessionLocalDataManager>();
        RegisterType<ComparisonResultPreparer>();
        BuildMoqContainer();
        
        var contextHelper = new TestContextGenerator(Container);
        contextHelper.GenerateSession();
        contextHelper.GenerateCurrentEndpoint();
        var testDirectory = _testDirectoryService.CreateTestDirectory();
        
        var mockEnvironmentService = Container.Resolve<Mock<IEnvironmentService>>();
        mockEnvironmentService.Setup(m => m.AssemblyFullName).Returns(IOUtils.Combine(testDirectory.FullName, "Assembly", "Assembly.exe"));
        
        var mockLocalApplicationDataManager = Container.Resolve<Mock<ILocalApplicationDataManager>>();
        mockLocalApplicationDataManager.Setup(m => m.ApplicationDataPath).Returns(IOUtils.Combine(testDirectory.FullName,
            "ApplicationDataPath"));
        
        _testDirectoryService.CreateTestDirectory();
    }
    
    [Test]
    public async Task Test_2_Inventories_Empty()
    {
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        ComparisonResult comparisonResult;
        
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        
        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);
        
        // Test
        var sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.FilesDirectories;
        sessionSettings.MatchingMode = MatchingModes.Tree;
        sessionSettings.LinkingCase = LinkingCases.Insensitive;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        comparisonResult.ComparisonItems.Should().HaveCount(0);
        
        // Test
        sessionSettings.DataType = DataTypes.Directories;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        comparisonResult.ComparisonItems.Should().HaveCount(0);
        
        // Test
        sessionSettings.DataType = DataTypes.Files;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        comparisonResult.ComparisonItems.Should().HaveCount(0);
        
        // Test
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.FilesDirectories;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        comparisonResult.ComparisonItems.Should().HaveCount(0);
        
        // Test
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Directories;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        comparisonResult.ComparisonItems.Should().HaveCount(0);
        
        // Test
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Files;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        comparisonResult.ComparisonItems.Should().HaveCount(0);
    }
    
    [Test]
    public async Task Test_2_Inventories_1_Same_File()
    {
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        ComparisonResult comparisonResult;
        
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        var file1 = _testDirectoryService.CreateFileInDirectory(dataA, "file1.txt", "contentA");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        file1.CopyTo(dataB.Combine(file1.Name));
        
        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);
        
        // Test
        var sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.FilesDirectories;
        sessionSettings.MatchingMode = MatchingModes.Tree;
        sessionSettings.LinkingCase = LinkingCases.Insensitive;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        DoChecks();
        
        // Test
        sessionSettings.DataType = DataTypes.Directories;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        DoChecksEmpty();
        
        // Test
        sessionSettings.DataType = DataTypes.Files;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        DoChecks();
        
        // Test
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.FilesDirectories;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        DoChecks();
        
        // Test
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Directories;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        DoChecksEmpty();
        
        // Test
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Files;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        DoChecks();
        
        void DoChecksEmpty()
        {
            comparisonResult.ComparisonItems.Should().HaveCount(0);
        }
        
        void DoChecks()
        {
            comparisonResult.ComparisonItems.Should().HaveCount(1);
            var comparisonItem = comparisonResult.ComparisonItems.Single();
            comparisonItem.ContentIdentities.Should().HaveCount(1);
            var contentIdentity = comparisonItem.ContentIdentities.Single();
            
            contentIdentity.Core!.SignatureHash.Should().BeNull();
            contentIdentity.Core.Size.Should().Be(8);
            
            contentIdentity.FileSystemDescriptions.Should().HaveCount(2);
            contentIdentity.InventoryPartsByLastWriteTimes.Should().HaveCount(1);
            contentIdentity.HasAnalysisError.Should().BeFalse();
            
            comparisonItem.PathIdentity.FileName.Should().Be("file1.txt");
            comparisonItem.PathIdentity.FileSystemType.Should().Be(FileSystemTypes.File);
            comparisonItem.PathIdentity.LinkingData.Should().Be("/file1.txt");
            comparisonItem.PathIdentity.LinkingKeyValue.Should().Be("/file1.txt");
            
            comparisonItem.ContentRepartition.FingerPrintGroups.Should().HaveCount(1);
            comparisonItem.ContentRepartition.LastWriteTimeGroups.Should().HaveCount(1);
            comparisonItem.ContentRepartition.MissingInventories.Should().BeEmpty();
            comparisonItem.ContentRepartition.MissingInventoryParts.Should().BeEmpty();
        }
    }
    
    [Test]
    public async Task Test_2_Inventories_1_File_Different()
    {
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        ComparisonResult comparisonResult;
        
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        _testDirectoryService.CreateFileInDirectory(dataA, "file1.txt", "contentA");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        _testDirectoryService.CreateFileInDirectory(dataB, "file1.txt",
            "contentB_"); // taille différente car lastwritetime peut être identique
        
        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);
        
        // Test
        var sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.FilesDirectories;
        sessionSettings.MatchingMode = MatchingModes.Tree;
        sessionSettings.LinkingCase = LinkingCases.Insensitive;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        DoChecks();
        
        // Test
        sessionSettings.DataType = DataTypes.Directories;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        comparisonResult.ComparisonItems.Should().HaveCount(0);
        
        // Test
        sessionSettings.DataType = DataTypes.Files;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        DoChecks();
        
        // Test
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.FilesDirectories;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        DoChecks();
        
        // Test
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Directories;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        comparisonResult.ComparisonItems.Should().HaveCount(0);
        
        // Test
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Files;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        DoChecks();
        
        void DoChecks()
        {
            comparisonResult.ComparisonItems.Should().HaveCount(1);
            var comparisonItem = comparisonResult.ComparisonItems.Single();
            comparisonItem.ContentIdentities.Should().HaveCount(2);
            var contentIdentityA = comparisonItem.ContentIdentities.Single(ci => ci.IsPresentIn(inventoryDataA.Inventory));
            var contentIdentityB = comparisonItem.ContentIdentities.Single(ci => ci.IsPresentIn(inventoryDataB.Inventory));
            
            contentIdentityA.Core!.SignatureHash.Should().NotBeNull();
            contentIdentityA.Core.SignatureHash.Should().NotBeEmpty();
            contentIdentityA.Core.Size.Should().Be(8);
            contentIdentityA.FileSystemDescriptions.Should().HaveCount(1);
            contentIdentityA.InventoryPartsByLastWriteTimes.Should().HaveCount(1);
            contentIdentityA.HasAnalysisError.Should().BeFalse();
            
            contentIdentityB.Core!.SignatureHash.Should().NotBeNull();
            contentIdentityB.Core.SignatureHash.Should().NotBeEmpty();
            contentIdentityB.Core.Size.Should().Be(9);
            contentIdentityB.FileSystemDescriptions.Should().HaveCount(1);
            contentIdentityB.InventoryPartsByLastWriteTimes.Should().HaveCount(1);
            contentIdentityB.HasAnalysisError.Should().BeFalse();
            
            comparisonItem.PathIdentity.FileName.Should().Be("file1.txt");
            comparisonItem.PathIdentity.FileSystemType.Should().Be(FileSystemTypes.File);
            comparisonItem.PathIdentity.LinkingData.Should().Be("/file1.txt");
            comparisonItem.PathIdentity.LinkingKeyValue.Should().Be("/file1.txt");
            
            comparisonItem.ContentRepartition.FingerPrintGroups.Should().HaveCount(2);
            comparisonItem.ContentRepartition.LastWriteTimeGroups.Count.Should().BeOneOf(1, 2);
            comparisonItem.ContentRepartition.MissingInventories.Should().BeEmpty();
            comparisonItem.ContentRepartition.MissingInventoryParts.Should().BeEmpty();
        }
    }
    
    [Test]
    public async Task Test_2_Inventories_1_Directory()
    {
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        ComparisonResult comparisonResult;
        
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        dataA.CreateSubdirectory("Dir1");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        dataB.CreateSubdirectory("Dir1");
        
        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);
        
        // Test
        var sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.FilesDirectories;
        sessionSettings.MatchingMode = MatchingModes.Tree;
        sessionSettings.LinkingCase = LinkingCases.Insensitive;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        DoChecks();
        
        // Test
        sessionSettings.DataType = DataTypes.Directories;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        DoChecks();
        
        // Test
        sessionSettings.DataType = DataTypes.Files;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        comparisonResult.ComparisonItems.Should().HaveCount(0);
        
        // Test
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.FilesDirectories;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        DoChecks();
        
        // Test
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Directories;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        DoChecks();
        
        // Test
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Files;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        comparisonResult.ComparisonItems.Should().HaveCount(0);
        
        void DoChecks()
        {
            comparisonResult.ComparisonItems.Should().HaveCount(1);
            var comparisonItem = comparisonResult.ComparisonItems.Single();
            comparisonItem.ContentIdentities.Should().HaveCount(1);
            var contentIdentity = comparisonItem.ContentIdentities.Single();
            
            contentIdentity.Core.Should().BeNull();
            contentIdentity.FileSystemDescriptions.Should().HaveCount(2);
            contentIdentity.InventoryPartsByLastWriteTimes.Should().HaveCount(0);
            contentIdentity.HasAnalysisError.Should().BeFalse();
            
            comparisonItem.PathIdentity.FileName.Should().Be("Dir1");
            comparisonItem.PathIdentity.FileSystemType.Should().Be(FileSystemTypes.Directory);
            comparisonItem.PathIdentity.LinkingData.Should().Be("/dir1");
            comparisonItem.PathIdentity.LinkingKeyValue.Should().Be("/Dir1");
            
            comparisonItem.ContentRepartition.FingerPrintGroups.Should().BeEmpty();
            comparisonItem.ContentRepartition.LastWriteTimeGroups.Should().BeEmpty();
            comparisonItem.ContentRepartition.MissingInventories.Should().BeEmpty();
            comparisonItem.ContentRepartition.MissingInventoryParts.Should().BeEmpty();
        }
    }
    
    [Test]
    [TestCase(AnalysisModes.Smart)]
    [TestCase(AnalysisModes.Checksum)]
    public async Task Test_2_AnalysisModes(AnalysisModes analysisMode)
    {
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        ComparisonResult comparisonResult;
        
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        var dirA1 = dataA.CreateSubdirectory("Dir1");
        for (var i = 0; i < 10; i++)
        {
            var fileInfo = _testDirectoryService.CreateFileInDirectory(dirA1, $"file_{i + 1}.txt", $"contents of file_{i + 1}.txt");
            fileInfo.LastWriteTime = DateTime.Today.AddDays(-1).AddHours(i);
        }
        
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        var dirB1 = dataB.CreateSubdirectory("Dir1");
        for (var i = 0; i < 10; i++)
        {
            var fileInfo = _testDirectoryService.CreateFileInDirectory(dirB1, $"file_{i + 1}.txt", $"contents of file_{i + 1}.txt");
            fileInfo.LastWriteTime = DateTime.Today.AddDays(-1).AddHours(i);
        }
        
        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);
        
        // Test
        var sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = analysisMode;
        sessionSettings.DataType = DataTypes.Files;
        sessionSettings.MatchingMode = MatchingModes.Tree;
        sessionSettings.LinkingCase = LinkingCases.Insensitive;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        
        comparisonResult.ComparisonItems.Should().HaveCount(10);
        foreach (var comparisonItem in comparisonResult.ComparisonItems)
        {
            comparisonItem.ContentIdentities.Should().HaveCount(1);
            var contentIdentity = comparisonItem.ContentIdentities.Single();
            
            contentIdentity.FileSystemDescriptions.Should().HaveCount(2);
            foreach (var fileSystemDescription in contentIdentity.FileSystemDescriptions)
            {
                var fileDescription = (FileDescription)fileSystemDescription;
                
                if (analysisMode == AnalysisModes.Smart)
                {
                    fileDescription.SignatureGuid.Should().BeNull();
                }
                else
                {
                    fileDescription.SignatureGuid.IsNotEmpty(true).Should().BeTrue();
                }
            }
            
            comparisonItem.PathIdentity.FileName.Should().StartWith("file_");
            comparisonItem.PathIdentity.FileName.Should().EndWith(".txt");
        }
    }
    
    [Test]
    [TestCase(AnalysisModes.Smart)]
    [TestCase(AnalysisModes.Checksum)]
    public async Task Test_2_AnalysisModes_2(AnalysisModes analysisMode)
    {
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        ComparisonResult comparisonResult;
        
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        var dirA1 = dataA.CreateSubdirectory("Dir1");
        for (var i = 0; i < 10; i++)
        {
            var fileInfo = _testDirectoryService.CreateFileInDirectory(dirA1, $"file_{i + 1}.txt", $"contents of file_{i + 1}.txt");
            fileInfo.LastWriteTime = DateTime.Today.AddDays(-2).AddHours(i);
        }
        
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        var dirB1 = dataB.CreateSubdirectory("Dir1");
        for (var i = 0; i < 10; i++)
        {
            var fileInfo = _testDirectoryService.CreateFileInDirectory(dirB1, $"file_{i + 1}.txt", $"contents of file_{i + 1}.txt");
            fileInfo.LastWriteTime = DateTime.Today.AddDays(-1).AddHours(i);
        }
        
        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);
        
        // Test
        var sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = analysisMode;
        sessionSettings.DataType = DataTypes.Files;
        sessionSettings.MatchingMode = MatchingModes.Tree;
        sessionSettings.LinkingCase = LinkingCases.Insensitive;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        
        comparisonResult.ComparisonItems.Should().HaveCount(10);
        foreach (var comparisonItem in comparisonResult.ComparisonItems)
        {
            comparisonItem.ContentIdentities.Should().HaveCount(1);
            var contentIdentity = comparisonItem.ContentIdentities.Single();
            
            contentIdentity.FileSystemDescriptions.Should().HaveCount(2);
            foreach (var fileSystemDescription in contentIdentity.FileSystemDescriptions)
            {
                var fileDescription = (FileDescription)fileSystemDescription;
                
                fileDescription.SignatureGuid.IsNotEmpty(true).Should().BeTrue();
            }
            
            // ClassicAssert.AreEqual(false, comparisonItem.ContentRepartition.IsOK);
            // ClassicAssert.AreEqual(false, comparisonItem.ContentRepartition.IsSuccessStatus);
            // ClassicAssert.AreEqual(false, comparisonItem.ContentRepartition.IsErrorStatus);
            
            comparisonItem.PathIdentity.FileName.Should().StartWith("file_");
            comparisonItem.PathIdentity.FileName.Should().EndWith(".txt");
        }
    }
    
    [Test]
    public async Task Test_PathIdentity_1()
    {
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        ComparisonResult comparisonResult;
        
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        var dirA1 = dataA.CreateSubdirectory("Dir1");
        _testDirectoryService.CreateFileInDirectory(dirA1, $"file_1.txt", $"contents of file_1.txt");
        
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        var dirB1 = dataB.CreateSubdirectory("Dir1");
        _testDirectoryService.CreateFileInDirectory(dirB1, $"file_1.txt", $"contents of file_1.txt");
        
        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);
        
        // Test
        var sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Files;
        sessionSettings.MatchingMode = MatchingModes.Tree;
        sessionSettings.LinkingCase = LinkingCases.Insensitive;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        
        comparisonResult.ComparisonItems.Should().HaveCount(1);
        var comparisonItem = comparisonResult.ComparisonItems.Single();
        var pathIdentity = comparisonItem.PathIdentity;
        pathIdentity.FileName.Should().Be("file_1.txt");
        pathIdentity.LinkingData.Should().Be("/dir1/file_1.txt");
        pathIdentity.LinkingKeyValue.Should().Be("/Dir1/file_1.txt");
        pathIdentity.FileSystemType.Should().Be(FileSystemTypes.File);
    }
    
    [Test]
    [TestCase(LinkingCases.Insensitive)]
    [TestCase(LinkingCases.Sensitive)]
    public async Task Test_PathIdentity_RelativePath_LinkingCases_1(LinkingCases linkingCase)
    {
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        ComparisonResult comparisonResult;
        
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        var dirA1 = dataA.CreateSubdirectory("Dir1");
        _testDirectoryService.CreateFileInDirectory(dirA1, $"file_1.txt", $"contents of file_1.txt");
        
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        var dirB1 = dataB.CreateSubdirectory("Dir1");
        _testDirectoryService.CreateFileInDirectory(dirB1, $"file_1.txt", $"contents of file_1.txt");
        
        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);
        
        // Test
        var sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Files;
        sessionSettings.MatchingMode = MatchingModes.Tree;
        sessionSettings.LinkingCase = linkingCase;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        
        comparisonResult.ComparisonItems.Should().HaveCount(1);
        var comparisonItem = comparisonResult.ComparisonItems.Single();
        var pathIdentity = comparisonItem.PathIdentity;
        pathIdentity.FileName.Should().Be("file_1.txt");
        if (linkingCase == LinkingCases.Insensitive)
        {
            pathIdentity.LinkingData.Should().Be("/dir1/file_1.txt");
        }
        else
        {
            pathIdentity.LinkingData.Should().Be("/Dir1/file_1.txt");
        }
        
        pathIdentity.LinkingKeyValue.Should().Be("/Dir1/file_1.txt");
        pathIdentity.FileSystemType.Should().Be(FileSystemTypes.File);
    }
    
    [Test]
    [TestCase(LinkingCases.Insensitive)]
    [TestCase(LinkingCases.Sensitive)]
    public async Task Test_PathIdentity_RelativePath_LinkingCases_2(LinkingCases linkingCase)
    {
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        ComparisonResult comparisonResult;
        
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        var dirA1 = dataA.CreateSubdirectory("Dir1");
        _testDirectoryService.CreateFileInDirectory(dirA1, $"file_1.txt", $"contents of file_1.txt");
        
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        var dirB1 = dataB.CreateSubdirectory("dir1");
        _testDirectoryService.CreateFileInDirectory(dirB1, $"file_1.txt", $"contents of file_1.txt");
        
        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);
        
        // Test
        var sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Files;
        sessionSettings.MatchingMode = MatchingModes.Tree;
        sessionSettings.LinkingCase = linkingCase;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        
        if (linkingCase == LinkingCases.Sensitive)
        {
            comparisonResult.ComparisonItems.Should().HaveCount(2);
            
            var pathIdentity1 = comparisonResult.ComparisonItems.Select(c => c.PathIdentity)
                .Single(p => p.LinkingData.Equals("/dir1/file_1.txt"));
            pathIdentity1.FileName.Should().Be("file_1.txt");
            pathIdentity1.LinkingKeyValue.Should().Be("/dir1/file_1.txt");
            pathIdentity1.FileSystemType.Should().Be(FileSystemTypes.File);
            
            var pathIdentity2 = comparisonResult.ComparisonItems.Select(c => c.PathIdentity)
                .Single(p => p.LinkingData.Equals("/Dir1/file_1.txt"));
            pathIdentity2.FileName.Should().Be("file_1.txt");
            pathIdentity2.LinkingKeyValue.Should().Be("/Dir1/file_1.txt");
            pathIdentity2.FileSystemType.Should().Be(FileSystemTypes.File);
        }
        else
        {
            comparisonResult.ComparisonItems.Should().HaveCount(1);
            var comparisonItem = comparisonResult.ComparisonItems.Single();
            var pathIdentity = comparisonItem.PathIdentity;
            pathIdentity.FileName.Should().Be("file_1.txt");
            pathIdentity.LinkingData.Should().Be("/dir1/file_1.txt");
            pathIdentity.LinkingKeyValue.Should().Be("/Dir1/file_1.txt");
            pathIdentity.FileSystemType.Should().Be(FileSystemTypes.File);
        }
    }
    
    //
    
    [Test]
    [TestCase(LinkingCases.Insensitive)]
    [TestCase(LinkingCases.Sensitive)]
    public async Task Test_PathIdentity_Name_LinkingCases_1(LinkingCases linkingCase)
    {
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        ComparisonResult comparisonResult;
        
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        var dirA1 = dataA.CreateSubdirectory("Dir1");
        _testDirectoryService.CreateFileInDirectory(dirA1, $"File_1.txt", $"contents of file_1.txt");
        
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        var dirB1 = dataB.CreateSubdirectory("Dir1");
        _testDirectoryService.CreateFileInDirectory(dirB1, $"File_1.txt", $"contents of file_1.txt");
        
        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);
        
        // Test
        var sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Files;
        sessionSettings.MatchingMode = MatchingModes.Flat;
        sessionSettings.LinkingCase = linkingCase;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        
        comparisonResult.ComparisonItems.Should().HaveCount(1);
        var comparisonItem = comparisonResult.ComparisonItems.Single();
        var pathIdentity = comparisonItem.PathIdentity;
        pathIdentity.FileName.Should().Be("File_1.txt");
        if (linkingCase == LinkingCases.Insensitive)
        {
            pathIdentity.LinkingData.Should().Be("file_1.txt");
        }
        else
        {
            pathIdentity.LinkingData.Should().Be("File_1.txt");
        }
        
        pathIdentity.LinkingKeyValue.Should().Be("File_1.txt");
        pathIdentity.FileSystemType.Should().Be(FileSystemTypes.File);
    }
    
    [Test]
    [TestCase(LinkingCases.Insensitive)]
    [TestCase(LinkingCases.Sensitive)]
    public async Task Test_PathIdentity_Name_LinkingCases_2(LinkingCases linkingCase)
    {
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        ComparisonResult comparisonResult;
        
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        var dirA1 = dataA.CreateSubdirectory("Dir1");
        _testDirectoryService.CreateFileInDirectory(dirA1, $"File_1.txt", $"contents of file_1.txt");
        
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        var dirB1 = dataB.CreateSubdirectory("dir1");
        _testDirectoryService.CreateFileInDirectory(dirB1, $"file_1.txt", $"contents of file_1.txt");
        
        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);
        
        // Test
        var sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Files;
        sessionSettings.MatchingMode = MatchingModes.Flat;
        sessionSettings.LinkingCase = linkingCase;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        
        if (linkingCase == LinkingCases.Sensitive)
        {
            comparisonResult.ComparisonItems.Should().HaveCount(2);
            
            var pathIdentity1 = comparisonResult.ComparisonItems.Select(c => c.PathIdentity)
                .Single(p => p.LinkingData.Equals("file_1.txt"));
            pathIdentity1.FileName.Should().Be("file_1.txt");
            pathIdentity1.LinkingKeyValue.Should().Be("file_1.txt");
            pathIdentity1.FileSystemType.Should().Be(FileSystemTypes.File);
            
            var pathIdentity2 = comparisonResult.ComparisonItems.Select(c => c.PathIdentity)
                .Single(p => p.LinkingData.Equals("File_1.txt"));
            pathIdentity2.FileName.Should().Be("File_1.txt");
            pathIdentity2.LinkingKeyValue.Should().Be("File_1.txt");
            pathIdentity2.FileSystemType.Should().Be(FileSystemTypes.File);
        }
        else
        {
            comparisonResult.ComparisonItems.Should().HaveCount(1);
            var comparisonItem = comparisonResult.ComparisonItems.Single();
            var pathIdentity = comparisonItem.PathIdentity;
            pathIdentity.FileName.Should().Be("File_1.txt");
            pathIdentity.LinkingData.Should().Be("file_1.txt");
            pathIdentity.LinkingKeyValue.Should().Be("File_1.txt");
            pathIdentity.FileSystemType.Should().Be(FileSystemTypes.File);
        }
    }
    
    [Test]
    [TestCase("subDirA")]
    [TestCase("subdira")]
    public async Task Test_PathIdentity_Folders_Insensitive(string subDirBName)
    {
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        ComparisonResult comparisonResult;
        
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        var dirA1 = dataA.CreateSubdirectory("Dir1");
        dirA1.CreateSubdirectory("subDirA");
        
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        var dirB1 = dataB.CreateSubdirectory("Dir1");
        dirB1.CreateSubdirectory(subDirBName);
        
        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);
        
        // Test
        var sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Directories;
        sessionSettings.MatchingMode = MatchingModes.Tree;
        sessionSettings.LinkingCase = LinkingCases.Insensitive;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        
        comparisonResult.ComparisonItems.Should().HaveCount(2);
        var comparisonItem = comparisonResult.ComparisonItems.Single(ci => ci.PathIdentity.FileName.Equals("Dir1"));
        var pathIdentity = comparisonItem.PathIdentity;
        pathIdentity.LinkingData.Should().Be("/dir1");
        pathIdentity.LinkingKeyValue.Should().Be("/Dir1");
        pathIdentity.FileSystemType.Should().Be(FileSystemTypes.Directory);
        
        comparisonItem = comparisonResult.ComparisonItems.Single(ci => ci.PathIdentity.FileName.Equals("subDirA"));
        pathIdentity = comparisonItem.PathIdentity;
        pathIdentity.LinkingData.Should().Be("/dir1/subdira");
        pathIdentity.LinkingKeyValue.Should().Be("/Dir1/subDirA");
        pathIdentity.FileSystemType.Should().Be(FileSystemTypes.Directory);
    }
}