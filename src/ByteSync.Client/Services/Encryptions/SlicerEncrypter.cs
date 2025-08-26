using System.IO;
using System.Security.Cryptography;
using System.Threading;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Services.Encryptions;

public class SlicerEncrypter : ISlicerEncrypter
{
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ILogger<SlicerEncrypter> _logger;
        
    private int _maxSliceLength;

    public SlicerEncrypter(ICloudSessionConnectionRepository cloudSessionConnectionRepository,
        ILogger<SlicerEncrypter> logger)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _logger = logger;
            
        BufferSize = 4096;
        MaxSliceLength = 1024 * 1024; // 1 Mb
    }
        
    public void Initialize(FileInfo fileToEncrypt, SharedFileDefinition sharedFileDefinition)
    {
        SharedFileDefinition = sharedFileDefinition;
            
        InStream = new FileStream(fileToEncrypt.FullName, FileMode.Open, FileAccess.Read);
        InReader = new BinaryReader(InStream);
        
        EndInitialize();
    }
        
    public void Initialize(MemoryStream memoryStream, SharedFileDefinition sharedFileDefinition)
    {
        SharedFileDefinition = sharedFileDefinition;

        memoryStream.Position = 0;
        InReader = new BinaryReader(memoryStream);
        
        EndInitialize();
    }

    private void EndInitialize()
    {
        Aes = Aes.Create();
        Aes.Key = _cloudSessionConnectionRepository.GetAesEncryptionKey()!;
        Aes.IV = SharedFileDefinition.IV;

        TotalGeneratedFiles = 0;
    }

    private SharedFileDefinition SharedFileDefinition { get; set; } = null!;

    private Stream InStream { get; set;} = null!;
        
    private BinaryReader InReader { get; set; } = null!;

    private Aes Aes { get; set; } = null!;
        
    private int TotalGeneratedFiles { get; set; }

    private int BufferSize { get; set; }
        
    public int MaxSliceLength
    {
        get { return _maxSliceLength; }
        set
        {
            _maxSliceLength = value;

            if (value < BufferSize)
            {
                BufferSize = value;
            }
        }
    }

    public async Task<FileUploaderSlice?> SliceAndEncrypt()
    {
        CryptoStream? cryptoStream = null;
        FileUploaderSlice? fileUploaderSlice = null;

        var bytes = InReader.ReadBytes(BufferSize);
        var readBytes = bytes.Length;
        var thisSessionReadBytes = readBytes;
            
        var canContinue = TotalGeneratedFiles == 0 || readBytes > 0;
        while (canContinue)
        {
            if (cryptoStream == null)
            {
                TotalGeneratedFiles += 1;

                var memoryStream = new MemoryStream();
                var encryptor = Aes.CreateEncryptor(Aes.Key, Aes.IV);
                cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write, true);

                fileUploaderSlice = new FileUploaderSlice(TotalGeneratedFiles, memoryStream);
            }
            
            await cryptoStream.WriteAsync(bytes.AsMemory(0, readBytes), CancellationToken.None);
           
            var sizeToRead = BufferSize;
            if (thisSessionReadBytes + sizeToRead > MaxSliceLength)
            {
                sizeToRead = MaxSliceLength - thisSessionReadBytes;
            }

            if (sizeToRead > 0)
            {
                bytes = InReader.ReadBytes(sizeToRead);
                readBytes = bytes.Length;
                thisSessionReadBytes += readBytes;

                if (readBytes == 0)
                {
                    canContinue = false;
                }
            }
            else
            {
                canContinue = false;
            }
        }

        cryptoStream?.Dispose();

        return fileUploaderSlice;
    }
        
    public void Dispose()
    {
        InStream?.Dispose();
        InReader?.Dispose();
        Aes?.Dispose();
        
        GC.SuppressFinalize(this);
    }
}