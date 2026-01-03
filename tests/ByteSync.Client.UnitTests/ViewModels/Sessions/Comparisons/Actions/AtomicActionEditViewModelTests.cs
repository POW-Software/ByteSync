using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Sessions.Comparisons.Actions;

[TestFixture]
public class AtomicActionEditViewModelTests
{
    private sealed class TestDataPartIndexer : IDataPartIndexer
    {
        private readonly ReadOnlyCollection<DataPart> _dataParts;
        
        public TestDataPartIndexer(IReadOnlyCollection<DataPart> dataParts)
        {
            _dataParts = new ReadOnlyCollection<DataPart>(dataParts.ToList());
        }
        
        public void BuildMap(List<Inventory> inventories)
        {
        }
        
        public ReadOnlyCollection<DataPart> GetAllDataParts()
        {
            return _dataParts;
        }
        
        public DataPart? GetDataPart(string? dataPartName)
        {
            return _dataParts.FirstOrDefault(dp => dp.Name == dataPartName);
        }
        
        public void Remap(ICollection<SynchronizationRule> synchronizationRules)
        {
        }
    }
    
    private static IDataPartIndexer BuildDataPartIndexer()
    {
        var endpoint = new ByteSyncEndpoint
        {
            ClientId = "c",
            ClientInstanceId = "ci",
            Version = "v",
            OSPlatform = OSPlatforms.Windows,
            IpAddress = "127.0.0.1"
        };
        
        var inventoryA = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = endpoint, MachineName = "M" };
        var inventoryB = new Inventory { InventoryId = "INV_B", Code = "B", Endpoint = endpoint, MachineName = "M" };
        var partA = new InventoryPart(inventoryA, "c:\\a", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventoryB, "c:\\b", FileSystemTypes.Directory) { Code = "B1" };
        
        var dataParts = new List<DataPart>
        {
            new("A", partA),
            new("B", partB)
        };
        
        return new TestDataPartIndexer(dataParts);
    }
    
    [Test]
    public void Constructor_ForFile_ShouldExposeExpectedActions()
    {
        var viewModel = new AtomicActionEditViewModel(FileSystemTypes.File, true, null, BuildDataPartIndexer());
        
        var actions = GetActionOperators(viewModel);
        
        actions.Should().Contain(ActionOperatorTypes.Copy);
        actions.Should().Contain(ActionOperatorTypes.CopyContentOnly);
        actions.Should().Contain(ActionOperatorTypes.CopyDatesOnly);
        actions.Should().Contain(ActionOperatorTypes.Delete);
        actions.Should().Contain(ActionOperatorTypes.DoNothing);
    }
    
    [Test]
    public void Constructor_ForDirectory_ShouldExposeExpectedActions()
    {
        var viewModel = new AtomicActionEditViewModel(FileSystemTypes.Directory, true, null, BuildDataPartIndexer());
        
        var actions = GetActionOperators(viewModel);
        
        actions.Should().Contain(ActionOperatorTypes.Create);
        actions.Should().Contain(ActionOperatorTypes.Delete);
        actions.Should().Contain(ActionOperatorTypes.DoNothing);
        actions.Should().NotContain(ActionOperatorTypes.Copy);
    }
    
    [Test]
    public void ExportSynchronizationAction_WhenMissingSelections_ShouldReturnNull()
    {
        var viewModel = new AtomicActionEditViewModel(FileSystemTypes.File, true, null, BuildDataPartIndexer());
        
        var result = InvokeExport(viewModel);
        
        result.Should().BeNull();
    }
    
    [Test]
    public void ExportSynchronizationAction_WithValidSelections_ShouldReturnAction()
    {
        var viewModel = new AtomicActionEditViewModel(FileSystemTypes.File, true, null, BuildDataPartIndexer());
        
        var action = GetActionByOperator(viewModel, ActionOperatorTypes.Copy);
        var sources = GetInternalEnumerable(viewModel, "Sources");
        var destinations = GetInternalEnumerable(viewModel, "Destinations");
        
        SetInternalProperty(viewModel, "SelectedAction", action);
        SetInternalProperty(viewModel, "SelectedSource", FirstItem(sources));
        SetInternalProperty(viewModel, "SelectedDestination", FirstItem(destinations));
        
        var result = InvokeExport(viewModel);
        
        result.Should().NotBeNull();
        result.Operator.Should().Be(ActionOperatorTypes.Copy);
        result.Source.Should().NotBeNull();
        result.Destination.Should().NotBeNull();
    }
    
    [Test]
    public void RemoveCommand_ShouldRaiseRemoveRequested()
    {
        var viewModel = new AtomicActionEditViewModel(FileSystemTypes.File, true, null, BuildDataPartIndexer());
        var raised = false;
        
        viewModel.RemoveRequested += (_, _) => raised = true;
        
        viewModel.RemoveCommand.Execute().Subscribe();
        
        raised.Should().BeTrue();
    }
    
    private static AtomicAction? InvokeExport(AtomicActionEditViewModel viewModel)
    {
        var method = typeof(AtomicActionEditViewModel).GetMethod("ExportSynchronizationAction",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return (AtomicAction?)method!.Invoke(viewModel, null);
    }
    
    private static IList<ActionOperatorTypes> GetActionOperators(AtomicActionEditViewModel viewModel)
    {
        var actions = GetInternalEnumerable(viewModel, "Actions");
        var results = new List<ActionOperatorTypes>();
        
        foreach (var action in actions)
        {
            var property = action!.GetType().GetProperty("ActionOperatorType", BindingFlags.Instance | BindingFlags.Public);
            results.Add((ActionOperatorTypes)property!.GetValue(action)!);
        }
        
        return results.ToList();
    }
    
    private static object GetActionByOperator(AtomicActionEditViewModel viewModel, ActionOperatorTypes operatorType)
    {
        var actions = GetInternalEnumerable(viewModel, "Actions");
        
        foreach (var action in actions)
        {
            var property = action!.GetType().GetProperty("ActionOperatorType", BindingFlags.Instance | BindingFlags.Public);
            var value = (ActionOperatorTypes)property!.GetValue(action)!;
            if (value == operatorType)
            {
                return action;
            }
        }
        
        throw new InvalidOperationException("Action not found");
    }
    
    private static IEnumerable GetInternalEnumerable(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        return (IEnumerable)property!.GetValue(target)!;
    }
    
    private static object FirstItem(IEnumerable items)
    {
        foreach (var item in items)
        {
            return item!;
        }
        
        throw new InvalidOperationException("Empty collection");
    }
    
    private static void SetInternalProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        property!.SetValue(target, value);
    }
}
