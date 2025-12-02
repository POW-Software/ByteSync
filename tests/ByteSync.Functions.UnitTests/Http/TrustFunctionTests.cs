using System.Net;
using System.Text;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Versions;
using ByteSync.Common.Controls.Json;
using ByteSync.Functions.Http;
using ByteSync.Functions.UnitTests.TestHelpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.Trusts;
using FluentAssertions;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Moq;

namespace ByteSync.Functions.UnitTests.Http;

[TestFixture]
public class TrustFunctionTests
{
    private static FunctionContext BuildFunctionContextWithClient()
    {
        var mockContext = new Mock<FunctionContext>();
        var items = new Dictionary<object, object>();
        mockContext.SetupGet(c => c.Items).Returns(items);
        
        var client = new Client("cli", "cliInst", "1.0.0", OSPlatforms.Windows, "127.0.0.1");
        items[AuthConstants.FUNCTION_CONTEXT_CLIENT] = client;
        
        return mockContext.Object;
    }
    
    [Test]
    public async Task InformProtocolVersionIncompatible_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();
        InformProtocolVersionIncompatibleRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<InformProtocolVersionIncompatibleRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (InformProtocolVersionIncompatibleRequest)r)
            .Returns(Task.CompletedTask);
        
        var function = new TrustFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        
        var request = new FakeHttpRequestData(context);
        var body = new InformProtocolVersionIncompatibleParameters
        {
            SessionId = "test-session-id",
            MemberClientInstanceId = "member-instance-id",
            JoinerClientInstanceId = "joiner-instance-id",
            MemberProtocolVersion = ProtocolVersion.CURRENT,
            JoinerProtocolVersion = 0
        };
        var json = JsonHelper.Serialize(body);
        await using (var writer = new StreamWriter(request.Body, Encoding.UTF8, 1024, leaveOpen: true))
        {
            await writer.WriteAsync(json);
        }
        
        request.Body.Position = 0;
        
        var response = await function.InformProtocolVersionIncompatible(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Parameters.Should().NotBeNull();
        captured.Parameters.SessionId.Should().Be("test-session-id");
        captured.Parameters.MemberClientInstanceId.Should().Be("member-instance-id");
        captured.Parameters.JoinerClientInstanceId.Should().Be("joiner-instance-id");
        captured.Parameters.MemberProtocolVersion.Should().Be(ProtocolVersion.CURRENT);
        captured.Parameters.JoinerProtocolVersion.Should().Be(0);
        captured.Client.Should().NotBeNull();
    }
}