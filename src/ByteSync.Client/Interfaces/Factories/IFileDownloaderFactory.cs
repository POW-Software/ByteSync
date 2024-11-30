using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Interfaces.Factories;

public interface IFileDownloaderFactory
{
    IFileDownloader Build(SharedFileDefinition sharedFileDefinition);
}