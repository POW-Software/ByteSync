using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.ViewModels.Misc;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Misc;

[TestFixture]
public class FlyoutContainerViewModelTests
{
    private Mock<ILocalizationService> _localizationServiceMock = null!;
    private Mock<IFlyoutElementViewModelFactory> _flyoutElementViewModelFactoryMock = null!;
    
    [SetUp]
    public void SetUp()
    {
        _localizationServiceMock = new Mock<ILocalizationService>();
        _flyoutElementViewModelFactoryMock = new Mock<IFlyoutElementViewModelFactory>();
        
        _localizationServiceMock
            .Setup(ls => ls[It.IsAny<string>()])
            .Returns((string key) => $"{key}-title");
    }
    
    private static DummyFlyoutElementViewModel CreateDummy() => new();
    
    [Test]
    public void ShowFlyout_Should_Set_Content_Title_And_Container()
    {
        // Arrange
        var vm = new FlyoutContainerViewModel(_localizationServiceMock.Object, _flyoutElementViewModelFactoryMock.Object);
        var content = CreateDummy();
        
        // Act
        vm.DoShowFlyoutInternal("TestKey", toggle: false, content);
        
        // Assert
        vm.Content.Should().BeSameAs(content);
        vm.IsFlyoutContainerVisible.Should().BeTrue();
        vm.Title.Should().Be("TestKey-title");
        content.Container.Should().BeSameAs(vm);
    }
    
    [Test]
    public void CloseCommand_Should_Close_When_CanClose_Is_True()
    {
        // Arrange
        var vm = new FlyoutContainerViewModel(_localizationServiceMock.Object, _flyoutElementViewModelFactoryMock.Object);
        var content = CreateDummy();
        vm.DoShowFlyoutInternal("Any", false, content);
        vm.CanCloseCurrentFlyout = true;
        
        // Sanity
        vm.CloseCommand.CanExecute
            .Take(1)
            .ToTask()
            .Result
            .Should().BeTrue();
        
        // Act
        vm.CloseCommand.Execute().Subscribe();
        
        // Assert
        vm.IsFlyoutContainerVisible.Should().BeFalse();
        vm.Content.Should().BeNull();
    }
    
    [Test]
    public void CloseCommand_Should_Not_Execute_When_CanClose_Is_False()
    {
        // Arrange
        var vm = new FlyoutContainerViewModel(_localizationServiceMock.Object, _flyoutElementViewModelFactoryMock.Object);
        vm.CanCloseCurrentFlyout = false;
        
        // Assert
        vm.CloseCommand.CanExecute
            .Take(1)
            .ToTask()
            .Result
            .Should().BeFalse();
    }
    
    [Test]
    public void DoCloseFlyout_Should_Close_Immediately()
    {
        // Arrange
        var vm = new FlyoutContainerViewModel(_localizationServiceMock.Object, _flyoutElementViewModelFactoryMock.Object);
        vm.Activator.Activate();
        var content = CreateDummy();
        vm.DoShowFlyoutInternal("Any", false, content);
        
        // Act: directly invoke internal close to avoid Dispatcher in unit tests
        var doClose = typeof(FlyoutContainerViewModel)
            .GetMethod("DoCloseFlyout", BindingFlags.Instance | BindingFlags.NonPublic);
        doClose!.Invoke(vm, null);
        
        // Assert
        vm.IsFlyoutContainerVisible.Should().BeFalse();
        vm.Content.Should().BeNull();
    }
    
    private class DummyFlyoutElementViewModel : FlyoutElementViewModel
    {
        public void TriggerCloseRequested()
        {
            RaiseCloseFlyoutRequested();
        }
    }
}