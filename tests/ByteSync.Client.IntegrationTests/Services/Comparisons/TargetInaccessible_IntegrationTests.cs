using System.Security.AccessControl;
using System.Security.Principal;
using Autofac;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Client.IntegrationTests.TestHelpers.Business;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Services.Comparisons;
using ByteSync.Services.Sessions;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.Services.Comparisons;

public class TargetInaccessible_IntegrationTests : IntegrationTest
{
    private ComparisonResultPreparer _comparisonResultPreparer = null!;
    private AtomicActionConsistencyChecker _checker = null!;
    
    [SetUp]
    public void Setup()
    {
        RegisterType<CloudSessionLocalDataManager, ICloudSessionLocalDataManager>();
        RegisterType<ComparisonResultPreparer>();
        RegisterType<AtomicActionConsistencyChecker>();
        BuildMoqContainer();
        
        var contextHelper = new TestContextGenerator(Container);
        contextHelper.GenerateSession();
        contextHelper.GenerateCurrentEndpoint();
        var testDirectory = _testDirectoryService.CreateTestDirectory();
        
        var env = Container.Resolve<Mock<IEnvironmentService>>();
        env.Setup(m => m.AssemblyFullName).Returns(IOUtils.Combine(testDirectory.FullName, "Assembly", "Assembly.exe"));
        
        var appData = Container.Resolve<Mock<ILocalApplicationDataManager>>();
        appData.Setup(m => m.ApplicationDataPath).Returns(IOUtils.Combine(testDirectory.FullName, "ApplicationDataPath"));
        
        // Ensure repository returns an empty set of existing actions
        Container.Resolve<Mock<IAtomicActionRepository>>()
            .Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>()))
            .Returns([]);
        
        _comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        _checker = Container.Resolve<AtomicActionConsistencyChecker>();
    }
    
    [Test]
    [Platform(Include = "Win")]
#pragma warning disable CA1416
    public async Task Synchronize_Fails_When_Target_File_Inaccessible_Windows()
    {
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        
        _ = _testDirectoryService.CreateFileInDirectory(dataA, "file.txt", "source");
        var fileB = _testDirectoryService.CreateFileInDirectory(dataB, "file.txt", "target");
        
        // Deny read access on target file for current user, then restore
        
        _ = fileB.GetAccessControl();
        var sid = WindowsIdentity.GetCurrent().User!;
        var denyRule = new FileSystemAccessRule(sid,
            FileSystemRights.ReadData | FileSystemRights.ReadAttributes | FileSystemRights.ReadExtendedAttributes,
            AccessControlType.Deny);
        
        try
        {
            var sec = fileB.GetAccessControl();
            sec.AddAccessRule(denyRule);
            fileB.SetAccessControl(sec);
            
            var settings = SessionSettingsGenerator.GenerateSessionSettings(DataTypes.Files, MatchingModes.Tree, AnalysisModes.Smart);
            var invA = new InventoryData(dataA);
            var invB = new InventoryData(dataB);
            var comparisonResult = await _comparisonResultPreparer.BuildAndCompare(settings, invA, invB);
            
            var targetItem = comparisonResult.ComparisonItems
                .Single(ci => ci.FileSystemType == FileSystemTypes.File && ci.PathIdentity.FileName == "file.txt");
            
            var action = new AtomicAction
            {
                Operator = ActionOperatorTypes.CopyContentOnly,
                Source = invA.GetSingleDataPart(),
                Destination = invB.GetSingleDataPart(),
                ComparisonItem = targetItem
            };
            
            var result = _checker.CheckCanAdd(action, targetItem);
            result.IsOK.Should().BeFalse();
            result.ValidationResults.Should().ContainSingle();
            result.ValidationResults[0].IsValid.Should().BeFalse();
            var reason = result.ValidationResults[0].FailureReason!.Value;
            reason.Should().BeOneOf(
                AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible,
                AtomicActionValidationFailureReason.AtLeastOneTargetsHasAnalysisError,
                AtomicActionValidationFailureReason.NothingToCopyContentAndDateIdentical);
        }
        finally
        {
            // Restore permissions to allow deletion
            var sec = fileB.GetAccessControl();
            sec.RemoveAccessRule(denyRule);
            fileB.SetAccessControl(sec);
        }
    }
#pragma warning restore CA1416
    
    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public async Task Synchronize_Fails_When_Target_File_Inaccessible_Posix()
    {
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        
        _ = _testDirectoryService.CreateFileInDirectory(dataA, "file.txt", "source");
        var fileB = _testDirectoryService.CreateFileInDirectory(dataB, "file.txt", "target");
        
        // Make file unreadable: chmod 000, then restore to 0644
        var path = fileB.FullName;
        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                File.SetUnixFileMode(path, UnixFileMode.None);
            }
            
            var settings = SessionSettingsGenerator.GenerateSessionSettings(DataTypes.Files, MatchingModes.Tree, AnalysisModes.Smart);
            var invA = new InventoryData(dataA);
            var invB = new InventoryData(dataB);
            var comparisonResult = await _comparisonResultPreparer.BuildAndCompare(settings, invA, invB);
            
            var targetItem = comparisonResult.ComparisonItems
                .Single(ci => ci.FileSystemType == FileSystemTypes.File && ci.PathIdentity.FileName == "file.txt");
            
            var action = new AtomicAction
            {
                Operator = ActionOperatorTypes.CopyContentOnly,
                Source = invA.GetSingleDataPart(),
                Destination = invB.GetSingleDataPart(),
                ComparisonItem = targetItem
            };
            
            var result = _checker.CheckCanAdd(action, targetItem);
            result.IsOK.Should().BeFalse();
            result.ValidationResults.Should().ContainSingle();
            result.ValidationResults[0].IsValid.Should().BeFalse();
            var reason = result.ValidationResults[0].FailureReason!.Value;
            reason.Should().BeOneOf(
                AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible,
                AtomicActionValidationFailureReason.AtLeastOneTargetsHasAnalysisError,
                AtomicActionValidationFailureReason.NothingToCopyContentAndDateIdentical);
        }
        finally
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                File.SetUnixFileMode(path,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            }
        }
    }
}