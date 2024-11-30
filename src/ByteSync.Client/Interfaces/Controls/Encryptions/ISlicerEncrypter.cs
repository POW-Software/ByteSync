using System.IO;
using System.Threading.Tasks;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Encryptions;

public interface ISlicerEncrypter : IDisposable
{
    void Initialize(FileInfo fileToEncrypt, SharedFileDefinition sharedFileDefinition);
    
    void Initialize(MemoryStream fileToEncrypt, SharedFileDefinition sharedFileDefinition);
    
    int MaxSliceLength { get; set; }
    
    Task<FileUploaderSlice?> SliceAndEncrypt();
}