using System.Net;
using System.Text;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Controls.Json;
using ByteSync.Functions.Http;
using ByteSync.Functions.UnitTests.TestHelpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.Inventories;
using FluentAssertions;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Moq;

namespace ByteSync.Functions.UnitTests.Http;

[TestFixture]
public class InventoryFunctionTests
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
    
    private static async Task WriteBodyAsync<T>(FakeHttpRequestData request, T body)
    {
        var json = JsonHelper.Serialize(body);
        var bytes = Encoding.UTF8.GetBytes(json);
        request.Body.SetLength(0);
        await request.Body.WriteAsync(bytes, 0, bytes.Length);
        request.Body.Position = 0;
    }

    [Test]
    public async Task Start_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        StartInventoryRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<StartInventoryRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (StartInventoryRequest)r)
            .ReturnsAsync(StartInventoryResult.BuildOK());
        
        var function = new InventoryFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var response = await function.Start(request, context, "S1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S1");
    }

    [Test]
    public async Task AddDataSource_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        AddDataSourceRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<AddDataSourceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (AddDataSourceRequest)r)
            .ReturnsAsync(true);
        
        var function = new InventoryFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var dataSource = new EncryptedDataSource { Id = "DS1", Data = [1] };
        await WriteBodyAsync(request, dataSource);
        
        var response = await function.AddDataSource(request, context, "S1", "CI1", "DN1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.DataNodeId.Should().Be("DN1");
    }

    [Test]
    public async Task RemoveDataSource_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        RemoveDataSourceRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<RemoveDataSourceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (RemoveDataSourceRequest)r)
            .ReturnsAsync(true);
        
        var function = new InventoryFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var dataSource = new EncryptedDataSource { Id = "DS1", Data = [1] };
        await WriteBodyAsync(request, dataSource);
        
        var response = await function.RemoveDataSource(request, context, "S1", "CI1", "DN1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.DataNodeId.Should().Be("DN1");
    }

    [Test]
    public async Task GetDataSources_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        GetDataSourcesRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDataSourcesRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (GetDataSourcesRequest)r)
            .ReturnsAsync([new EncryptedDataSource { Id = "DS1" }]);
        
        var function = new InventoryFunction(mediatorMock.Object);
        var context = new Mock<FunctionContext>().Object;
        var request = new FakeHttpRequestData(context);
        
        var response = await function.GetDataSources(request, context, "S1", "CI1", "DN1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.DataNodeId.Should().Be("DN1");
    }

    [Test]
    public async Task AddDataNode_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        AddDataNodeRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<AddDataNodeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (AddDataNodeRequest)r)
            .ReturnsAsync(true);
        
        var function = new InventoryFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var dataNode = new EncryptedDataNode { Id = "DN1", Data = [1] };
        await WriteBodyAsync(request, dataNode);
        
        var response = await function.AddDataNode(request, context, "S1", "CI1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.ClientInstanceId.Should().Be("CI1");
    }

    [Test]
    public async Task RemoveDataNode_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        RemoveDataNodeRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<RemoveDataNodeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (RemoveDataNodeRequest)r)
            .ReturnsAsync(true);
        
        var function = new InventoryFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var dataNode = new EncryptedDataNode { Id = "DN1", Data = [1] };
        await WriteBodyAsync(request, dataNode);
        
        var response = await function.RemoveDataNode(request, context, "S1", "CI1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.ClientInstanceId.Should().Be("CI1");
    }
    
    [Test]
    public async Task GetDataNodes_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        GetDataNodesRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDataNodesRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (GetDataNodesRequest)r)
            .ReturnsAsync([new EncryptedDataNode { Id = "DN1" }]);
        
        var function = new InventoryFunction(mediatorMock.Object);
        var context = new Mock<FunctionContext>().Object;
        var request = new FakeHttpRequestData(context);
        
        var response = await function.GetDataNodes(request, context, "S1", "CI1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.ClientInstanceId.Should().Be("CI1");
    }
}
