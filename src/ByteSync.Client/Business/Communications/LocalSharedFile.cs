using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Business.Communications;

public class LocalSharedFile
{
    public LocalSharedFile(SharedFileDefinition sharedFileDefinition, string localPath)
    {
        SharedFileDefinition = sharedFileDefinition;
            
        LocalPath = localPath;
    }
        
    public SharedFileDefinition SharedFileDefinition { get; set; }
        
    public string LocalPath { get; }
}