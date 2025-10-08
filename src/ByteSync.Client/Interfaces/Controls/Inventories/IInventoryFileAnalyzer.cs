using System.IO;
using System.Threading;
using ByteSync.Business;
using ByteSync.Models.FileSystems;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IInventoryFileAnalyzer
{
    bool IsAllIdentified { get; set; }
    
    ManualResetEvent HasFinished { get; }
    
    void Start();
    void Stop();
    void RegisterFile(FileDescription fileDescription, FileInfo fileInfo);
    
    void Initialize(FingerprintModes mode, IInventorySaver saver, Action<FileDescription> onAnalyzed, Action<FileDescription> onError);
}