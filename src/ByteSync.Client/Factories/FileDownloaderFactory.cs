using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Factories;
using ByteSync.Services.Communications.Transfers;

namespace ByteSync.Factories;

public class FileDownloaderFactory : IFileDownloaderFactory
{
    private readonly IPolicyFactory _policyFactory;
    private readonly IDownloadTargetBuilder _downloadTargetBuilder;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly IMergerDecrypterFactory _mergerDecrypterFactory;

    public FileDownloaderFactory(IPolicyFactory policyFactory, IDownloadTargetBuilder downloadTargetBuilder,
        IFileTransferApiClient fileTransferApiClient, IMergerDecrypterFactory mergerDecrypterFactory)
    {
        _policyFactory = policyFactory;
        _downloadTargetBuilder = downloadTargetBuilder;
        _fileTransferApiClient = fileTransferApiClient;
        _mergerDecrypterFactory = mergerDecrypterFactory;
    }
    
    public IFileDownloader Build(SharedFileDefinition sharedFileDefinition)
    {
        return new FileDownloader(sharedFileDefinition, 
            _policyFactory, _downloadTargetBuilder, _fileTransferApiClient, _mergerDecrypterFactory);
    }
}