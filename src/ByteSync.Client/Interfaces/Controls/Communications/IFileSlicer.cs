using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFileSlicer
{
    Task SliceAndEncryptAsync(SharedFileDefinition sharedFileDefinition, UploadProgressState progressState, 
        int? maxSliceLength = null);
} 

