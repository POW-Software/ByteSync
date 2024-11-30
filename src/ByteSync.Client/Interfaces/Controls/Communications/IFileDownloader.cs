using System.Threading.Tasks;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFileDownloader : IDisposable
{
    Task AddAvailablePartAsync(int partNumber);

    Task SetAllAvailablePartsKnownAsync(int partsCount);

    Task WaitForFileFullyExtracted();
    
    DownloadTarget DownloadTarget { get; }
    
    SharedFileDefinition SharedFileDefinition { get; }
}