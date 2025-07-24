using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers.Strategies;

public class DownloadStrategyFactory : IDownloadStrategyFactory
{
    private readonly IDictionary<string, IDownloadStrategy> _strategies;

    public DownloadStrategyFactory()
    {
        _strategies = new Dictionary<string, IDownloadStrategy>(StringComparer.OrdinalIgnoreCase)
        {
            { "BlobStorage", new BlobStorageDownloadStrategy() },
            { "CloudFlare", new CloudFlareDownloadStrategy() }
        };
    }

    public IDownloadStrategy GetStrategy(FileSourceInfo downloadInfo)
    {
        if (_strategies.TryGetValue(downloadInfo.StorageMode, out var strategy))
        {
            return strategy;
        }

        // Default to BlobStorage if unknown mode
        return _strategies["BlobStorage"];
    }
} 