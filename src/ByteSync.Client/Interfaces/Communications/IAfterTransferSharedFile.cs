using System.Threading.Tasks;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Communications;

public interface IAfterTransferSharedFile
{
    Task OnFilePartUploaded(SharedFileDefinition sharedFileDefinition);
    
    Task OnUploadFinished(SharedFileDefinition sharedFileDefinition);
    Task OnFilePartUploadedError(SharedFileDefinition sharedFileDefinition, Exception exception);
    Task OnUploadFinishedError(SharedFileDefinition sharedFileDefinition, Exception exception);
}