using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Services.Sessions.Connecting.Joining;

public interface ICloudSessionPasswordExchangeKeyGivenService
{
    Task Process(GiveCloudSessionPasswordExchangeKeyParameters request);
}