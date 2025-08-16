using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace ByteSync.Services.Encryptions;

public class MergerDecrypter : IMergerDecrypter, IDisposable
{
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ILogger<MergerDecrypter> _logger;

    public MergerDecrypter(string localPath, DownloadTarget downloadTarget, CancellationTokenSource cancellationTokenSource, 
        ICloudSessionConnectionRepository cloudSessionConnectionRepository, ILogger<MergerDecrypter> logger)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _logger = logger;
            
        TotalReadFiles = 0;

        Initialize(localPath, downloadTarget, cancellationTokenSource);
    }
        
    private int TotalReadFiles { get; set; }

    public string FinalFile { get; private set; } = null!;

    public SharedFileDefinition SharedFileDefinition { get; private set; } = null!;

    private Aes? Aes { get; set; }
        
    private DownloadTarget DownloadTarget { get; set; } = null!;
        
    private CancellationTokenSource CancellationTokenSource { get; set; } = null!; 
        
    private void Initialize(string finalFile, DownloadTarget downloadTarget, CancellationTokenSource cancellationTokenSource)
    {
        FinalFile = finalFile;

        DownloadTarget = downloadTarget;
        SharedFileDefinition = downloadTarget.SharedFileDefinition;
            
        Aes = Aes.Create();
        
        var encryptionKey = _cloudSessionConnectionRepository.GetAesEncryptionKey();
        if (encryptionKey == null)
        {
            throw new InvalidOperationException("Encryption key is not available");
        }
        
        if (SharedFileDefinition.IV == null || SharedFileDefinition.IV.Length == 0)
        {
            throw new InvalidOperationException("Invalid IV provided");
        }
        
        Aes.Key = encryptionKey;
        Aes.IV = SharedFileDefinition.IV;

        CancellationTokenSource = cancellationTokenSource;

        var fileInfo = new FileInfo(FinalFile);
        if (fileInfo.Directory != null)
        {
            fileInfo.Directory.Create();
        }
        using (var _ = new FileStream(FinalFile, FileMode.Append))
        {
            // Pour créer au moins un fichier vide
        }
    }

    public async Task MergeAndDecrypt()
    {
        if (CancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        if (Aes == null)
        {
            throw new InvalidOperationException("AES encryption not initialized");
        }
            
        await using var outStream = new FileStream(FinalFile, FileMode.Append);
            
        using var cryptoTransform = Aes.CreateDecryptor(Aes.Key, Aes.IV);
        await using var cryptoStream = new CryptoStream(outStream, cryptoTransform, CryptoStreamMode.Write);
            
        TotalReadFiles += 1;
        _logger.LogDebug("MergeAndDecrypt memoryStream {Number}", TotalReadFiles);
        var memoryStream = DownloadTarget.GetMemoryStream(TotalReadFiles);

        
        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(cryptoStream, CancellationTokenSource.Token);
    }

    public void Dispose()
    {
        Aes?.Dispose();
    }
}