using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFileUploadPreparer
{
    void PrepareUpload(SharedFileDefinition sharedFileDefinition, long length);
} 