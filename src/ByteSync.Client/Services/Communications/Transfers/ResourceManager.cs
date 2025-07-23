using ByteSync.Business.Communications.Downloading;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers;

public class ResourceManager : IResourceManager
{
    
    private readonly DownloadPartsInfo _downloadPartsInfo;
    private readonly DownloadTarget _downloadTarget;

    public ResourceManager(DownloadPartsInfo downloadPartsInfo, DownloadTarget downloadTarget)
    {
        _downloadPartsInfo = downloadPartsInfo;
        _downloadTarget = downloadTarget;
    }

    public void Cleanup()
    {
        _downloadPartsInfo.Clear();
        _downloadTarget.ClearMemoryStream();
    }
    
} 