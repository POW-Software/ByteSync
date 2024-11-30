using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class AskCloudSessionPasswordExchangeKeyPush
{
    public string SessionId { get; set; }
    public PublicKeyInfo PublicKeyInfo { get; set; }
    public string RequesterInstanceId { get; set; }
}