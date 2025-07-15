using System.Threading.Tasks;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Services.Communications.Transfers;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IDownloadManager
{
    Task OnFilePartReadyToDownload(SharedFileDefinition tupleSharedFileDefinition, int tuplePartNumber);

    Task OnFileReadyToFinalize(SharedFileDefinition sharedFileDefinition, int tuplePartsCount);

    void RegisterPartsCoordinator(SharedFileDefinition sharedFileDefinition, IDownloadPartsCoordinator coordinator);

    IFileDownloaderCache FileDownloaderCache { get; }
}