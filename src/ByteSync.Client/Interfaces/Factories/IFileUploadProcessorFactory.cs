using System.IO;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Encryptions;

namespace ByteSync.Interfaces.Factories;

public interface IFileUploadProcessorFactory
{
    IFileUploadProcessor Create(
        ISlicerEncrypter slicerEncrypter,
        string? localFileToUpload,
        MemoryStream? memoryStream,
        SharedFileDefinition sharedFileDefinition);
} 