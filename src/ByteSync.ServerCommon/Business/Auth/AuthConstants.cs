namespace ByteSync.ServerCommon.Business.Auth;

public static class AuthConstants
{
    public const string ISSUER = "PowByteSync";
    public const string AUDIENCE = "PowByteSyncUsers";
    public const string BYTESYNCUSER = "ByteSyncUser";
    public const string CLAIMBASEDAUTH = "ClaimBasedAuth";
    
    public const string CLAIM_IP_ADDRESS = "ipAddress";
    public const string CLAIM_CLIENT_ID = "clientId";
    public const string CLAIM_CLIENT_INSTANCE_ID = "clientInstanceId";
    public const string CLAIM_VERSION = "version";
    public const string CLAIM_OS_PLATFORM = "osPlatform";
    
    public const string FUNCTION_CONTEXT_CLIENT = "Client";
}