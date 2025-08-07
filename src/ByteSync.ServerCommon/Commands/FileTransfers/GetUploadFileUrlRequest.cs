using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.FileTransfers;

public class GetUploadFileUrlRequest : IRequest<string>
{
    public GetUploadFileUrlRequest(string sessionId, Client client, TransferParameters transferParameters)
    {
        SessionId = sessionId;
        Client = client;
        TransferParameters = transferParameters;
    }

    public string SessionId { get; set; }
    public Client Client { get; set; }
    public TransferParameters TransferParameters { get; set; }
} 