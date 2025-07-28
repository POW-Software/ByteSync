using Autofac.Features.Indexed;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers.Strategies;

public class DownloadStrategyService : IDownloadStrategyFactory
{
    private readonly IIndex<StorageProvider, IDownloadStrategy> _strategies;

    public DownloadStrategyService(IIndex<StorageProvider, IDownloadStrategy> strategies)
    {
        _strategies = strategies;
    }

    public IDownloadStrategy GetStrategy(FileStorageLocation storageLocation)
    {
        if (_strategies.TryGetValue(storageLocation.StorageProvider, out var strategy))
        {
            return strategy;
        }

        // Default to AzureBlobStorage if unknown mode
        return _strategies[StorageProvider.AzureBlobStorage];
    }
} 