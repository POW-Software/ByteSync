using System.IO;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Interfaces.Factories;

public interface IFileUploaderFactory
{
    IFileUploader Build(string fullName, SharedFileDefinition sharedFileDefinition);
    
    IFileUploader Build(MemoryStream memoryStream, SharedFileDefinition sharedFileDefinition);
}