using ByteSync.Interfaces.Controls.Encryptions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers;

public class FileMerger : IFileMerger
{
    
    private readonly List<IMergerDecrypter> _mergerDecrypters;
    private readonly IErrorManager _errorManager;
    private readonly DownloadTarget _downloadTarget;
    private readonly SemaphoreSlim _semaphoreSlim;

    public FileMerger(List<IMergerDecrypter> mergerDecrypters, IErrorManager errorManager,
        DownloadTarget downloadTarget, SemaphoreSlim semaphoreSlim)
    {
        _mergerDecrypters = mergerDecrypters;
        _errorManager = errorManager;
        _downloadTarget = downloadTarget;
        _semaphoreSlim = semaphoreSlim;
    }
    
    public async Task MergeAsync(int partToMerge)
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            try
            {
                foreach (var mergerDecrypter in _mergerDecrypters)
                {
                    try
                    {
                        await mergerDecrypter.MergeAndDecrypt();
                    }
                    finally
                    {
                        mergerDecrypter.Dispose();
                    }
                }

                _downloadTarget.RemoveMemoryStream(partToMerge);
            }
            catch (InvalidOperationException ex)
            {
                // Log security-related exceptions without exposing sensitive details
                await _errorManager.SetOnErrorAsync();
                throw new InvalidOperationException("Encryption operation failed", ex);
            }
            catch (CryptographicException ex)
            {
                // Handle cryptographic failures securely
                await _errorManager.SetOnErrorAsync();
                throw new InvalidOperationException("Cryptographic operation failed", ex);
            }
            catch (Exception ex)
            {
                await _errorManager.SetOnErrorAsync();
                throw new InvalidOperationException("Merge operation failed", ex);
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }

    }
    
} 