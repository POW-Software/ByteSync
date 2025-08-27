using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFileSlicer
{
	// TODO : remove this method and its implementation
    Task SliceAndEncryptAsync(SharedFileDefinition sharedFileDefinition, UploadProgressState progressState, 
        int? maxSliceLength = null);

	Task SliceAndEncryptAdaptiveAsync(SharedFileDefinition sharedFileDefinition, UploadProgressState progressState);
} 

