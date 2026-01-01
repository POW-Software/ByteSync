using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Business.Themes;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Factories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.Services.Comparisons;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Sessions.Comparisons.Results;

[TestFixture]
public class ContentRepartitionViewModelLetteringTests
{
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
    
    private class LetteringComputer : IContentRepartitionGroupsComputer
    {
        private readonly ContentRepartitionViewModel _vm;
        private readonly List<Inventory> _inventories;
        
        public LetteringComputer(ContentRepartitionViewModel vm, List<Inventory> inventories)
        {
            _vm = vm;
            _inventories = inventories;
        }
        
        public ContentRepartitionComputeResult Compute()
        {
            var result = new ContentRepartitionComputeResult(_vm.FileSystemType);
            
            var onlyOnePart = _inventories.All(i => i.InventoryParts.Count == 1);
            
            // Use FingerPrintGroups for files
            if (_vm.FileSystemType == FileSystemTypes.File)
            {
                _vm.FingerPrintGroups!.Clear();
                var members = _vm.ContentRepartition.FingerPrintGroups.SelectMany(p => p.Value).OrderBy(ip => ip.Code);
                foreach (var ip in members)
                {
                    var s = new StatusItemViewModel
                    {
                        Letter = onlyOnePart ? ip.Inventory.Code : ip.Code,
                        BackBrushColor = ContentRepartitionViewModel.BrushColors.MainBackground,
                        ForeBrushColor = ContentRepartitionViewModel.BrushColors.MainForeColor
                    };
                    _vm.FingerPrintGroups.Add(s);
                }
                
                result.FingerPrintGroups = 1;
            }
            
            return result;
        }
    }
    
    private class TestFactory : IContentRepartitionGroupsComputerFactory
    {
        private readonly Func<ContentRepartitionViewModel, IContentRepartitionGroupsComputer> _builder;
        
        public TestFactory(Func<ContentRepartitionViewModel, IContentRepartitionGroupsComputer> builder)
        {
            _builder = builder;
        }
        
        public IContentRepartitionGroupsComputer Build(ContentRepartitionViewModel contentRepartitionViewModel) =>
            _builder(contentRepartitionViewModel);
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
    
    [Test]
    public void InitialBadges_ShowPartCodes_WhenAnyInventoryHasMultipleParts()
    {
        // Arrange: A has 1 part, B has 2 parts
        var invA = MakeInventory("A", 1);
        var invB = MakeInventory("B", 2);
        var allInventories = new List<Inventory> { invA, invB };
        
        // Build a file ComparisonItem with a single identity containing all parts
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "f", "f", "f");
        var item = new ComparisonItem(pathIdentity) { ComparisonResult = new ComparisonResult() };
        var core = new ContentIdentityCore { SignatureHash = "h", Size = 1 };
        var identity = new ContentIdentity(core);
        item.AddContentIdentity(identity);
        
        // Populate ContentRepartition with parts A1, B1, B2 in the same fingerprint group
        var rep = item.ContentRepartition;
        rep.FingerPrintGroups[core] = new HashSet<InventoryPart>
        {
            invA.InventoryParts[0],
            invB.InventoryParts[0],
            invB.InventoryParts[1]
        };
        
        var theme = new TestThemeService();
        var factory = new TestFactory(vm => new LetteringComputer(vm, allInventories));
        
        // Act
        var vm = new ContentRepartitionViewModel(item, allInventories, theme, factory);
        
        // Assert: letters should be A1, B1, B2 (not A, B, B)
        vm.FingerPrintGroups.Should().NotBeNull();
        vm.FingerPrintGroups!.Select(s => s.Letter).Should().ContainInOrder("A1", "B1", "B2");
    }
    
    [Test]
    public void LastWriteTime_Badges_UseWidthProportionalToMultiplicity()
    {
        // Arrange: two occurrences for A at the same timestamp, one for B
        var invA = MakeInventory("A", 1);
        var invB = MakeInventory("B", 1);
        var allInventories = new List<Inventory> { invA, invB };

        var pathIdentity = new PathIdentity(FileSystemTypes.File, "f", "f", "f");
        var item = new ComparisonItem(pathIdentity) { ComparisonResult = new ComparisonResult() };

        var date = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        item.ContentRepartition.LastWriteTimeGroups[date] = new List<InventoryPart>
        {
            invA.InventoryParts[0],
            invA.InventoryParts[0],
            invB.InventoryParts[0]
        };

        var theme = new TestThemeService();
        var factory = new TestFactory(vm => new ContentRepartitionGroupsComputer(vm, allInventories));

        // Act
        var vm = new ContentRepartitionViewModel(item, allInventories, theme, factory);

        // Assert
        vm.LastWriteTimeGroups.Should().NotBeNull();
        vm.LastWriteTimeGroups!.Select(s => (s.Letter, s.Width)).Should()
            .ContainInOrder(
                ("A", StatusItemViewModel.BaseWidth * 2),
                ("B", StatusItemViewModel.BaseWidth));
    }
}
