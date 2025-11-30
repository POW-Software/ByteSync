namespace ByteSync.Common.Business.Versions;

public static class ProtocolVersion
{
    public const int V1 = 1;
    
    public const int Current = V1;
    
    public const int MinSupported = V1;
    
    public static bool IsCompatible(int otherVersion)
    {
        if (otherVersion == 0)
        {
            return false;
        }
        
        return otherVersion == Current;
    }
}

