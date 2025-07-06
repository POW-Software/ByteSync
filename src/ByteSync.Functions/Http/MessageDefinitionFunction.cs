using System.Net;
using ByteSync.ServerCommon.Commands.Messages;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ByteSync.Functions.Http;

public class MessageDefinitionFunction
{
    private readonly IMediator _mediator;

    public MessageDefinitionFunction(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Function("GetMessages")]
    public async Task<HttpResponseData> GetMessages(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "messages")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var messages = await _mediator.Send(new GetActiveMessagesRequest());

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(messages, HttpStatusCode.OK);
        return response;
    }
}
