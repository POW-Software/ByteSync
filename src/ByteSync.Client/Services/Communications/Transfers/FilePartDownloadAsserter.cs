using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications.Http;
using Serilog;
using System.Threading;

namespace ByteSync.Services.Communications.Transfers;

public class FilePartDownloadAsserter : IFilePartDownloadAsserter
{
    
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly Action _onError;

    public FilePartDownloadAsserter(IFileTransferApiClient fileTransferApiClient, SemaphoreSlim semaphoreSlim, Action onError)
    {
        _fileTransferApiClient = fileTransferApiClient;
        _semaphoreSlim = semaphoreSlim;
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
            await _semaphoreSlim.WaitAsync();
            try
            {
                _onError();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
    
} 