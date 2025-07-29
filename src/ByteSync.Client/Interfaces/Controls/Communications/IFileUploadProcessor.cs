using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFileUploadProcessor
{
    Task ProcessUpload(SharedFileDefinition sharedFileDefinition, int? maxSliceLength = null);
    
    int GetTotalCreatedSlices();
    
    int GetMaxConcurrentUploads();
} 