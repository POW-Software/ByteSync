using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.ServerCommon.Interfaces.Services.Storage;

namespace ByteSync.Client.IntegrationTests.TestHelpers;

public class R2FileTransferApiClient : IFileTransferApiClient
{
    private readonly ICloudflareR2Service _r2Service;

    public R2FileTransferApiClient(ICloudflareR2Service r2Service)
    {
        _r2Service = r2Service;
    }

    public async Task<string> GetUploadFileUrl(TransferParameters transferParameters)
    {
        return await _r2Service.GetUploadFileUrl(transferParameters.SharedFileDefinition, transferParameters.PartNumber!.Value);
    }

    public async Task<FileStorageLocation> GetUploadFileStorageLocation(TransferParameters transferParameters)
    {
        var url = await GetUploadFileUrl(transferParameters);
        return new FileStorageLocation(url, StorageProvider.CloudflareR2);
    }

    public async Task<string> GetDownloadFileUrl(TransferParameters transferParameters)
    {
        return await _r2Service.GetDownloadFileUrl(transferParameters.SharedFileDefinition, transferParameters.PartNumber!.Value);
    }

    public async Task<FileStorageLocation> GetDownloadFileStorageLocation(TransferParameters transferParameters)
    {
        var url = await GetDownloadFileUrl(transferParameters);
        return new FileStorageLocation(url, StorageProvider.CloudflareR2);
    }

    public Task AssertFilePartIsUploaded(TransferParameters transferParameters)
    {
        // No-op in client-only integration
        return Task.CompletedTask;
    }

    public Task AssertUploadIsFinished(TransferParameters transferParameters)
    {
        // No-op in client-only integration
        return Task.CompletedTask;
    }

    public Task AssertFilePartIsDownloaded(TransferParameters transferParameters)
    {
        // No-op in client-only integration
        return Task.CompletedTask;
    }

    public Task AssertDownloadIsFinished(TransferParameters transferParameters)
    {
        // No-op in client-only integration
        return Task.CompletedTask;
    }
}


