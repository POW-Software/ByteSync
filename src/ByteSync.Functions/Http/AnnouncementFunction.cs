using System.Net;
using ByteSync.ServerCommon.Commands.Announcements;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ByteSync.Functions.Http;

public class AnnouncementFunction
{
    private readonly IMediator _mediator;

    public AnnouncementFunction(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Function("GetAnnouncements")]
    public async Task<HttpResponseData> GetAnnouncements(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "announcements")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var announcements = await _mediator.Send(new GetActiveAnnouncementsRequest());

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(announcements, HttpStatusCode.OK);
        return response;
    }
}
