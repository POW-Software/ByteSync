using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.FileTransfers;

public class GetUploadFileUrlRequest : IRequest<string>
{
    public GetUploadFileUrlRequest(string sessionId, Client client, SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        SessionId = sessionId;
        Client = client;
        SharedFileDefinition = sharedFileDefinition;
        PartNumber = partNumber;
    }

    public string SessionId { get; set; }
    public Client Client { get; set; }
    public SharedFileDefinition SharedFileDefinition { get; set; }
    public int PartNumber { get; set; }
} 