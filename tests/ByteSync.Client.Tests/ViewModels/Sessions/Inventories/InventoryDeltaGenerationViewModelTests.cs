using Avalonia.Media;
using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.ViewModels.Sessions.Inventories;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.ViewModels.Sessions.Inventories;

[TestFixture]
public class InventoryDeltaGenerationViewModelTests
{
    private InventoryProcessData _processData = null!;
    private Mock<IInventoryService> _inventoryService = null!;
    private Mock<IThemeService> _themeService = null!;
    private IBrush _backgroundBrush = null!;
    private IBrush _secondaryBrush = null!;
    
    [SetUp]
    public void Setup()
    {
        _processData = new InventoryProcessData();
        
        _inventoryService = new Mock<IInventoryService>();
        _inventoryService.SetupGet(x => x.InventoryProcessData).Returns(_processData);
        
        _backgroundBrush = Mock.Of<IBrush>();
        _secondaryBrush = Mock.Of<IBrush>();
        
        _themeService = new Mock<IThemeService>(MockBehavior.Strict);
        _themeService.Setup(x => x.GetBrush("HomeCloudSynchronizationBackGround")).Returns(_backgroundBrush);
        _themeService.Setup(x => x.GetBrush("MainSecondaryColor")).Returns(_secondaryBrush);
    }
    
    [Test]
    public void Activation_BindsAnalysisStatus_AndInitialValues()
    {
        var vm = CreateVm();
        
        vm.AnalysisStatus.Should().Be(InventoryTaskStatus.Pending);
        vm.IsAnalysisRunning.Should().BeFalse();
        vm.HasAnalysisStarted.Should().BeFalse();
        
        vm.AnalyzeErrors.Should().Be(0);
        vm.HasErrors.Should().BeFalse();
        vm.AnalyzedFiles.Should().Be(0);
        vm.AnalyzableFiles.Should().Be(0);
        vm.AnalyzeSuccess.Should().Be(0);
        vm.ProcessedSize.Should().Be(0);
        vm.AnalysisIconBrush.Should().BeSameAs(_backgroundBrush);
        
        _processData.AnalysisStatus.OnNext(InventoryTaskStatus.Running);
        
        vm.AnalysisStatus.Should().Be(InventoryTaskStatus.Running);
        vm.IsAnalysisRunning.Should().BeTrue();
        vm.HasAnalysisStarted.Should().BeTrue();
        
        _processData.AnalysisStatus.OnNext(InventoryTaskStatus.Success);
        
        vm.AnalysisStatus.Should().Be(InventoryTaskStatus.Success);
        vm.IsAnalysisRunning.Should().BeFalse();
        vm.HasAnalysisStarted.Should().BeTrue("flag stays true after first run");
    }
    
    [Test]
    public void MonitorUpdates_RefreshCountersAndDerivedValues()
    {
        var vm = CreateVm();
        
        _processData.UpdateMonitorData(m =>
        {
            m.AnalyzeErrors = 2;
            m.AnalyzedFiles = 5;
            m.AnalyzableFiles = 8;
            m.ProcessedSize = 42;
        });
        
        WaitUntil(() => vm.AnalyzedFiles == 5, "view model should project latest monitor data");
        
        vm.AnalyzedFiles.Should().Be(5);
        vm.AnalyzeErrors.Should().Be(2);
        vm.AnalyzableFiles.Should().Be(8);
        vm.ProcessedSize.Should().Be(42);
        vm.AnalyzeSuccess.Should().Be(3);
        vm.HasErrors.Should().BeTrue();
        
        _processData.UpdateMonitorData(m =>
        {
            m.AnalyzeErrors = 0;
            m.AnalyzedFiles = 9;
        });
        
        WaitUntil(() => vm.HasErrors == false, "errors flag should follow monitor data");
        
        vm.AnalyzedFiles.Should().Be(9);
        vm.AnalyzeErrors.Should().Be(0);
        vm.AnalyzeSuccess.Should().Be(9);
        vm.HasErrors.Should().BeFalse();
    }
    
    [Test]
    public void StatusTransitions_UpdateIconsBrushesAndText()
    {
        var vm = CreateVm();
        
        vm.AnalysisIcon.Should().Be("None");
        vm.AnalysisIconBrush.Should().BeSameAs(_backgroundBrush);
        vm.AnalysisStatusText.Should().NotBeNullOrWhiteSpace();
        
        _processData.AnalysisStatus.OnNext(InventoryTaskStatus.Running);
        
        vm.AnalysisStatus.Should().Be(InventoryTaskStatus.Running);
        vm.IsAnalysisRunning.Should().BeTrue();
        vm.AnalysisIcon.Should().Be("None");
        vm.AnalysisIconBrush.Should().BeSameAs(_backgroundBrush);
        vm.AnalysisStatusText.Should().NotBeNullOrWhiteSpace();
        
        _processData.AnalysisStatus.OnNext(InventoryTaskStatus.Cancelled);
        
        vm.AnalysisStatus.Should().Be(InventoryTaskStatus.Cancelled);
        vm.IsAnalysisRunning.Should().BeFalse();
        vm.AnalysisIcon.Should().Be("SolidXCircle");
        vm.AnalysisIconBrush.Should().BeSameAs(_secondaryBrush);
        var cancelledText = vm.AnalysisStatusText;
        cancelledText.Should().NotBeNullOrWhiteSpace();
        
        vm.AnalyzeErrors = 2;
        _processData.AnalysisStatus.OnNext(InventoryTaskStatus.Success);
        
        vm.AnalysisStatus.Should().Be(InventoryTaskStatus.Success);
        vm.AnalysisIcon.Should().Be("RegularError");
        vm.AnalysisIconBrush.Should().BeSameAs(_secondaryBrush);
        var successWithErrorsText = vm.AnalysisStatusText;
        successWithErrorsText.Should().NotBeNullOrWhiteSpace();
        successWithErrorsText.Should().NotBe(cancelledText);
        
        vm.AnalyzeErrors = 0;
        _processData.AnalysisStatus.OnNext(InventoryTaskStatus.Success);
        
        vm.AnalysisIcon.Should().Be("SolidCheckCircle");
        vm.AnalysisIconBrush.Should().BeSameAs(_backgroundBrush);
        var successText = vm.AnalysisStatusText;
        successText.Should().NotBeNullOrWhiteSpace();
        successText.Should().NotBe(successWithErrorsText);
        
        _processData.AnalysisStatus.OnNext(InventoryTaskStatus.Error);
        
        vm.AnalysisStatus.Should().Be(InventoryTaskStatus.Error);
        vm.AnalysisIcon.Should().Be("SolidXCircle");
        vm.AnalysisIconBrush.Should().BeSameAs(_secondaryBrush);
        vm.AnalysisStatusText.Should().NotBeNullOrWhiteSpace();
        vm.AnalysisStatusText.Should().NotBe(successText);
    }
    
    private InventoryDeltaGenerationViewModel CreateVm()
    {
        var vm = new InventoryDeltaGenerationViewModel(_inventoryService.Object, _themeService.Object);
        vm.Activator.Activate();
        
        return vm;
    }
    
    private static void WaitUntil(Func<bool> condition, string because)
    {
        SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(2)).Should().BeTrue(because);
    }
}