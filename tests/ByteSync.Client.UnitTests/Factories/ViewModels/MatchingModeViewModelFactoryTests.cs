using Autofac;
using ByteSync.Business.Sessions;
using ByteSync.Factories.ViewModels;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.ViewModels.Sessions.Managing;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Factories.ViewModels;

[TestFixture]
public class MatchingModeViewModelFactoryTests
{
    private IContainer _container = null!;
    
    [SetUp]
    public void SetUp()
    {
        var localization = new Mock<ILocalizationService>();
        localization.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => key);
        
        var builder = new ContainerBuilder();
        builder.RegisterInstance(localization.Object).As<ILocalizationService>();
        builder.RegisterType<MatchingModeViewModel>();
        
        _container = builder.Build();
    }
    
    [TearDown]
    public void TearDown()
    {
        _container.Dispose();
    }
    
    [Test]
    public void CreateMatchingModeViewModel_Flat_ReturnsConfiguredViewModel()
    {
        var factory = new MatchingModeViewModelFactory(_container);
        
        var vm = factory.CreateMatchingModeViewModel(MatchingModes.Flat);
        
        vm.Should().NotBeNull();
        vm.MatchingMode.Should().Be(MatchingModes.Flat);
        vm.Description.Should().NotBeNullOrEmpty();
    }
    
    [Test]
    public void CreateMatchingModeViewModel_Tree_ReturnsConfiguredViewModel()
    {
        var factory = new MatchingModeViewModelFactory(_container);
        
        var vm = factory.CreateMatchingModeViewModel(MatchingModes.Tree);
        
        vm.Should().NotBeNull();
        vm.MatchingMode.Should().Be(MatchingModes.Tree);
        vm.Description.Should().NotBeNullOrEmpty();
    }
}