namespace ByteSync.Common.Business.Versions;

public static class ProtocolVersion
{
    public const int V1 = 1;
    public const int V2 = 2;
    
    public const int V3 = 3;
    public const int CURRENT = V3;
    
    public const int MIN_SUPPORTED = V3;
    
    public static bool IsCompatible(int otherVersion)
    {
        if (otherVersion == 0)
        {
            return false;
        }
        
        return otherVersion == CURRENT;
    }
}
