using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Models.FileSystems;
using ByteSync.Services.Inventories;

namespace ByteSync.Interfaces.Factories;

public interface IInventoryFileAnalyzerFactory
{
    IInventoryFileAnalyzer Create(InventoryBuilder builder, Action<FileDescription> onAnalyzed, Action<FileDescription> onError);
}