using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFilePartUploadAsserter
{
    Task AssertFilePartIsUploaded(SharedFileDefinition sharedFileDefinition, int partNumber);
    Task AssertUploadIsFinished(SharedFileDefinition sharedFileDefinition, int totalParts);
} 