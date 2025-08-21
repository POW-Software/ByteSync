using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFileDownloader
{
    Task StartDownload();
    
    Task WaitForFileFullyExtracted();
    
    DownloadTarget DownloadTarget { get; }
    
    SharedFileDefinition SharedFileDefinition { get; }

    IDownloadPartsCoordinator PartsCoordinator { get; }
}