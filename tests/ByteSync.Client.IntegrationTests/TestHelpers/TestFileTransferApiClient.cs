using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Client.IntegrationTests.TestHelpers;

public class TestFileTransferApiClient : IFileTransferApiClient
{
    public Task<string> GetUploadFileUrl(TransferParameters transferParameters)
    {
        return Task.FromResult(transferParameters.SharedFileDefinition.Id);
    }

    public async Task<FileStorageLocation> GetUploadFileStorageLocation(TransferParameters transferParameters)
    {
        var url = await GetUploadFileUrl(transferParameters);
        return new FileStorageLocation(url, StorageProvider.AzureBlobStorage);
    }

    public Task<string> GetDownloadFileUrl(TransferParameters transferParameters) => Task.FromResult(string.Empty);
    public Task<FileStorageLocation> GetDownloadFileStorageLocation(TransferParameters transferParameters) => Task.FromResult(new FileStorageLocation(string.Empty, StorageProvider.AzureBlobStorage));
    public Task AssertFilePartIsUploaded(TransferParameters transferParameters) => Task.CompletedTask;
    public Task AssertUploadIsFinished(TransferParameters transferParameters) => Task.CompletedTask;
    public Task AssertFilePartIsDownloaded(TransferParameters transferParameters) => Task.CompletedTask;
    public Task AssertDownloadIsFinished(TransferParameters transferParameters) => Task.CompletedTask;
}


