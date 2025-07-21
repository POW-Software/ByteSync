using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IDownloadTargetBuilder
{
    DownloadTarget BuildDownloadTarget(SharedFileDefinition sharedFileDefinition);
    
    void ClearCache();
}