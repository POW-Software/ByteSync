using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.FileTransfers;

public class AssertDownloadIsFinishedRequest : IRequest
{
    public AssertDownloadIsFinishedRequest(string sessionId, Client client, SharedFileDefinition sharedFileDefinition)
    {
        SessionId = sessionId;
        Client = client;
        SharedFileDefinition = sharedFileDefinition;
    }

    public string SessionId { get; set; }
    public Client Client { get; set; }
    public SharedFileDefinition SharedFileDefinition { get; set; }
} 