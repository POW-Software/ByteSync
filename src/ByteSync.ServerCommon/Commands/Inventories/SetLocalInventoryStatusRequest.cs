using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class SetLocalInventoryStatusRequest : IRequest<bool>
{
    public SetLocalInventoryStatusRequest(Client client, UpdateSessionMemberGeneralStatusParameters parameters)
    {
        Client = client;
        Parameters = parameters;
    }
    
    public Client Client { get; }
    
    public UpdateSessionMemberGeneralStatusParameters Parameters { get; }
}