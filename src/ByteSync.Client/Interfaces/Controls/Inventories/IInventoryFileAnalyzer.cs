using System.IO;
using System.Threading;
using ByteSync.Models.FileSystems;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IInventoryFileAnalyzer
{
    bool IsAllIdentified { get; set; }
    
    ManualResetEvent HasFinished { get; }
    
    void Start();
    void Stop();
    void RegisterFile(FileDescription fileDescription, FileInfo fileInfo);
}