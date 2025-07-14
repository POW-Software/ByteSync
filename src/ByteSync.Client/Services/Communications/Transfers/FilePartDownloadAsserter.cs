using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications.Http;
using Serilog;

namespace ByteSync.Services.Communications.Transfers;

public class FilePartDownloadAsserter : IFilePartDownloadAsserter
{
    
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly object _syncRoot;
    private readonly Action _onError;

    public FilePartDownloadAsserter(IFileTransferApiClient fileTransferApiClient, object syncRoot, Action onError)
    {
        _fileTransferApiClient = fileTransferApiClient;
        _syncRoot = syncRoot;
        _onError = onError;
    }

    public async Task AssertAsync(TransferParameters parameters)
    {
        try
        {
            await Task.Run(() => _fileTransferApiClient.AssertFilePartIsDownloaded(parameters));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AssertFilePartIsDownloaded - SharedFileType:{SharedFileType} - PartNumber:{PartNumber}", 
                parameters.SharedFileDefinition?.SharedFileType, parameters.PartNumber);
            lock (_syncRoot)
            {
                _onError();
            }
        }
    }
    
} 