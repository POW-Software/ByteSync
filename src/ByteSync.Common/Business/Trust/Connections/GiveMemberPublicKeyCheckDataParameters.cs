using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Common.Business.Trust.Connections;

public class GiveMemberPublicKeyCheckDataParameters
{
    public PublicKeyCheckData PublicKeyCheckData { get; set; } = null!;

    public string ClientInstanceId { get; set; } = null!;
    
    public string SessionId { get; set; } = null!;
}