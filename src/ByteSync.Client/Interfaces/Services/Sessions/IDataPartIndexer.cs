using System.Collections.ObjectModel;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Models.Inventories;

namespace ByteSync.Interfaces.Services.Sessions;

public interface IDataPartIndexer
{
    public void BuildMap(List<Inventory> inventories);

    public ReadOnlyCollection<DataPart> GetAllDataParts();
    
    public DataPart? GetDataPart(DataPart? dataPart);

    public DataPart? GetDataPart(string? dataPartName);

    void Remap(ICollection<SynchronizationRule> synchronizationRules);
}