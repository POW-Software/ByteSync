using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IDownloadStrategyFactory
{
    IDownloadStrategy GetStrategy(FileStorageLocation storageLocation);
} 