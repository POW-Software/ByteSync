using System.IO;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Interfaces.Factories;

public interface IFileUploadProcessorFactory
{
    IFileUploadProcessor Create(
        string? localFileToUpload,
        MemoryStream? memoryStream,
        SharedFileDefinition sharedFileDefinition);
} 