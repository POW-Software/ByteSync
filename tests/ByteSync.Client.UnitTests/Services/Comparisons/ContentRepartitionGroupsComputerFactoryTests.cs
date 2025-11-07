using System.Reactive.Linq;
using Autofac;
using Avalonia.Media;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Business.Themes;
using ByteSync.Common.Business.Inventories;
using ByteSync.Factories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Factories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Comparisons;

[TestFixture]
public class ContentRepartitionGroupsComputerFactoryTests
{
    private class CaptureComputer : IContentRepartitionGroupsComputer
    {
        public CaptureComputer(ContentRepartitionViewModel vm, List<Inventory> inventories)
        {
            ViewModel = vm;
            ReceivedInventories = inventories;
        }
        
        public ContentRepartitionViewModel ViewModel { get; }
        
        public List<Inventory> ReceivedInventories { get; }
        
        public ContentRepartitionComputeResult Compute()
        {
            // No-op for this test
            return new ContentRepartitionComputeResult(ViewModel.FileSystemType);
        }
    }
    
    private class NoOpComputer : IContentRepartitionGroupsComputer
    {
        private readonly ContentRepartitionViewModel _vm;
        
        public NoOpComputer(ContentRepartitionViewModel vm)
        {
            _vm = vm;
        }
        
        public ContentRepartitionComputeResult Compute()
        {
            return new ContentRepartitionComputeResult(_vm.FileSystemType);
        }
    }
    
    private class NoOpFactory : IContentRepartitionGroupsComputerFactory
    {
        public IContentRepartitionGroupsComputer Build(ContentRepartitionViewModel contentRepartitionViewModel)
        {
            return new NoOpComputer(contentRepartitionViewModel);
        }
    }
    
    private class TestThemeService : IThemeService
    {
        public List<Theme> AvailableThemes { get; } = new();
        
        public IObservable<Theme> SelectedTheme { get; } = Observable.Return(new Theme("Unit", ThemeModes.Light,
            new ThemeColor(Colors.White), new ThemeColor(Colors.White)));
        
        public void OnThemesRegistered()
        {
        }
        
        public void RegisterTheme(Theme theme)
        {
        }
        
        public IBrush? GetBrush(string resourceName) => new SolidColorBrush(Colors.White);
        
        public void SelectTheme(string? name, bool isDarkMode)
        {
        }
    }
    
    private static Inventory MakeInventory(string code, int parts)
    {
        var inv = new Inventory { Code = code, InventoryId = Guid.NewGuid().ToString("N") };
        for (int i = 1; i <= parts; i++)
        {
            var part = new InventoryPart(inv, $"/{code}{i}", FileSystemTypes.Directory) { Code = $"{code}{i}" };
            inv.Add(part);
        }
        
        return inv;
    }
    
    private static ContentRepartitionViewModel BuildVm(List<Inventory> allInventories)
    {
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "f", "f", "f");
        var compItem = new ComparisonItem(pathIdentity)
        {
            ComparisonResult = new ComparisonResult()
        };
        compItem.ComparisonResult.AddInventory(allInventories[0]);
        
        var theme = new TestThemeService();
        var noOpFactory = new NoOpFactory();
        
        return new ContentRepartitionViewModel(compItem, allInventories, theme, noOpFactory);
    }
    
    [Test]
    public void Factory_Uses_ViewModel_Inventories()
    {
        // Arrange: global list A(1 part), B(2 parts)
        var globalInventories = new List<Inventory>
        {
            MakeInventory("A", 1),
            MakeInventory("B", 2)
        };
        
        // Service list (should NOT be used)
        var serviceInventories = new List<Inventory> { MakeInventory("S", 1) };
        
        var builder = new ContainerBuilder();
        
        var invServiceMock = new Mock<IInventoryService>();
        invServiceMock.SetupGet(s => s.InventoryProcessData)
            .Returns(new InventoryProcessData());
        builder.RegisterInstance(invServiceMock.Object);
        
        // Register our capturing implementation
        builder.RegisterType<CaptureComputer>().As<IContentRepartitionGroupsComputer>();
        var container = builder.Build();
        var context = container.Resolve<IComponentContext>();
        
        var factory = new ContentRepartitionGroupsComputerFactory(context);
        var vm = BuildVm(globalInventories);
        
        // Act
        var comp = factory.Build(vm);
        
        // Assert
        comp.Should().BeOfType<CaptureComputer>();
        var capture = (CaptureComputer)comp;
        capture.ReceivedInventories.Should().BeSameAs(globalInventories);
        capture.ReceivedInventories.Should().NotBeSameAs(serviceInventories);
    }
}