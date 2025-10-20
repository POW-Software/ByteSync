using System.Collections.ObjectModel;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Models.Inventories;

namespace ByteSync.Interfaces.Services.Sessions;

public interface IDataPartIndexer
{
    void BuildMap(List<Inventory> inventories);
    
    ReadOnlyCollection<DataPart> GetAllDataParts();
    
    DataPart? GetDataPart(string? dataPartName);
    
    void Remap(ICollection<SynchronizationRule> synchronizationRules);
}