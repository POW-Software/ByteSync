using System.Threading.Tasks;
using ByteSync.Business.Communications;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IPostDownloadHandlerProxy
{
    Task HandleDownloadFinished(LocalSharedFile? downloadTargetLocalSharedFile);
}