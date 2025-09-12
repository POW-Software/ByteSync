using System.Net;
using System.Text;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Functions.Http;
using ByteSync.Functions.UnitTests.TestHelpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.Synchronizations;
using FluentAssertions;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Moq;

namespace ByteSync.Functions.UnitTests.Http;

[TestFixture]
public class SynchronizationFunctionTests
{
    private static FunctionContext BuildFunctionContextWithClient()
    {
        var mockContext = new Mock<FunctionContext>();
        var items = new Dictionary<object, object>();
        mockContext.SetupGet(c => c.Items).Returns(items);

        var client = new Client("cli", "cliInst", "1.0.0", ByteSync.Common.Business.Misc.OSPlatforms.Windows, "127.0.0.1");
        items[AuthConstants.FUNCTION_CONTEXT_CLIENT] = client;

        return mockContext.Object;
    }

    [Test]
    public async Task LocalCopyIsDone_ForwardsMetrics_AndReturnsOk()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        LocalCopyIsDoneRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<LocalCopyIsDoneRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (LocalCopyIsDoneRequest)r)
            .Returns(Task.CompletedTask);

        var function = new SynchronizationFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();

        var request = new FakeHttpRequestData(context);
        var body = new SynchronizationActionRequest
        {
            ActionsGroupIds = new List<string> { "A1" },
            NodeId = "N1",
            ActionMetricsByActionId = new Dictionary<string, SynchronizationActionMetrics>
            {
                ["A1"] = new SynchronizationActionMetrics { TransferredBytes = 1234 }
            }
        };
        var json = ByteSync.Common.Controls.Json.JsonHelper.Serialize(body);
        await using (var writer = new StreamWriter(request.Body, Encoding.UTF8, 1024, leaveOpen: true))
        {
            await writer.WriteAsync(json);
        }
        request.Body.Position = 0;

        // Act
        var response = await function.LocalCopyIsDone(request, context, "S1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S1");
        captured.ActionsGroupIds.Should().ContainSingle().Which.Should().Be("A1");
        captured.NodeId.Should().Be("N1");
        captured.ActionMetricsByActionId.Should().NotBeNull();
        captured.ActionMetricsByActionId!.Should().ContainKey("A1");
        captured.ActionMetricsByActionId!["A1"].TransferredBytes.Should().Be(1234);
    }

    [Test]
    public async Task DateIsCopied_ReturnsOk_AndSendsRequest()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        DateIsCopiedRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<DateIsCopiedRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (DateIsCopiedRequest)r)
            .Returns(Task.CompletedTask);

        var function = new SynchronizationFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();

        var request = new FakeHttpRequestData(context);
        var body = new SynchronizationActionRequest
        {
            ActionsGroupIds = new List<string> { "B1" },
            NodeId = "N2"
        };
        var json = ByteSync.Common.Controls.Json.JsonHelper.Serialize(body);
        await using (var writer = new StreamWriter(request.Body, Encoding.UTF8, 1024, leaveOpen: true))
        {
            await writer.WriteAsync(json);
        }
        request.Body.Position = 0;

        // Act
        var response = await function.DateIsCopied(request, context, "S2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S2");
        captured.ActionsGroupIds.Should().ContainSingle().Which.Should().Be("B1");
        captured.NodeId.Should().Be("N2");
    }
}
