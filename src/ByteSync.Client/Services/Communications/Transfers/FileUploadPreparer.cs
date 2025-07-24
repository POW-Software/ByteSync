using System.Security.Cryptography;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers;

public class FileUploadPreparer : IFileUploadPreparer
{
    public void PrepareUpload(SharedFileDefinition sharedFileDefinition, long length)
    {
        using (var aes = Aes.Create())
        {
            aes.GenerateIV();
            sharedFileDefinition.IV = aes.IV;
        }
        
        sharedFileDefinition.UploadedFileLength = length;
    }
} 