using System.Threading.Tasks;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IDownloadManager
{
    Task OnFilePartReadyToDownload(SharedFileDefinition tupleSharedFileDefinition, int tuplePartNumber);

    Task OnFileReadyToFinalize(SharedFileDefinition sharedFileDefinition, int tuplePartsCount);
}