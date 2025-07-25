using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Functions.Helpers.Misc;
using ByteSync.ServerCommon.Commands.SessionMembers;
using MediatR;

namespace ByteSync.Functions.Http;

public class SessionMemberFunction
{
    private readonly IMediator _mediator;

    public SessionMemberFunction(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Function("SessionMemberSetGeneralStatusFunction")]
    public async Task<HttpResponseData> SetGeneralStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/sessionMember/{clientInstanceId}/generalStatus")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var generalStatusParameters = await FunctionHelper.DeserializeRequestBody<UpdateSessionMemberGeneralStatusParameters>(req);
            
        var request = new SetGeneralStatusRequest(client, generalStatusParameters);
        var result = await _mediator.Send(request);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        
        return response;
    }
} 