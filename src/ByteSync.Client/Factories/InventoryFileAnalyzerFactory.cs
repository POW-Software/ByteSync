using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories;
using ByteSync.Models.FileSystems;
using ByteSync.Services.Inventories;

namespace ByteSync.Factories;

public class InventoryFileAnalyzerFactory : IInventoryFileAnalyzerFactory
{
    public IInventoryFileAnalyzer Create(InventoryBuilder builder, Action<FileDescription> onAnalyzed, Action<FileDescription> onError)
    {
        var analyzer = new InventoryFileAnalyzer();
        analyzer.Initialize(builder.FingerprintMode, builder.InventorySaver, onAnalyzed, onError);
        
        return analyzer;
    }
}