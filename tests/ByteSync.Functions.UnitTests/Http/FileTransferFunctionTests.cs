using System.Net;
using System.Text;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Controls.Json;
using ByteSync.Functions.Http;
using ByteSync.Functions.UnitTests.TestHelpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.FileTransfers;
using FluentAssertions;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Moq;

namespace ByteSync.Functions.UnitTests.Http;

[TestFixture]
public class FileTransferFunctionTests
{


    [Test]
    public async Task GetUploadFileUrl_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        GetUploadFileUrlRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUploadFileUrlRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (GetUploadFileUrlRequest)r)
            .ReturnsAsync("http://upload.url");
        
        var function = new FileTransferFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new TransferParameters { SharedFileDefinition = new SharedFileDefinition { Id = "F1", SharedFileType = SharedFileTypes.FullInventory } };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.GetUploadFileUrl(request, context, "S1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S1");
    }

    [Test]
    public async Task GetUploadFileStorageLocation_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        GetUploadFileStorageLocationRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUploadFileStorageLocationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (GetUploadFileStorageLocationRequest)r)
            .ReturnsAsync(new FileStorageLocation("http://abc", StorageProvider.AzureBlobStorage));
        
        var function = new FileTransferFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new TransferParameters { SharedFileDefinition = new SharedFileDefinition { Id = "F1" } };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.GetUploadFileStorageLocation(request, context, "S1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S1");
    }
    
    [Test]
    public async Task GetDownloadFileUrl_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        GetDownloadFileUrlRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDownloadFileUrlRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (GetDownloadFileUrlRequest)r)
            .ReturnsAsync("http://download.url");
        
        var function = new FileTransferFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new TransferParameters { SharedFileDefinition = new SharedFileDefinition { Id = "F1" } };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.GetDownloadFileUrl(request, context, "S1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S1");
    }

    [Test]
    public async Task GetDownloadFileStorageLocation_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        GetDownloadFileStorageLocationRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDownloadFileStorageLocationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (GetDownloadFileStorageLocationRequest)r)
            .ReturnsAsync(new FileStorageLocation("http://xyz", StorageProvider.AzureBlobStorage));
        
        var function = new FileTransferFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new TransferParameters { SharedFileDefinition = new SharedFileDefinition { Id = "F1" } };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.GetDownloadFileStorageLocation(request, context, "S1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S1");
    }

    [Test]
    public async Task AssertFilePartIsUploaded_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        AssertFilePartIsUploadedRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<AssertFilePartIsUploadedRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (AssertFilePartIsUploadedRequest)r)
            .Returns(Task.CompletedTask);
        
        var function = new FileTransferFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new TransferParameters { SharedFileDefinition = new SharedFileDefinition { Id = "F1" } };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.AssertFilePartIsUploaded(request, context, "S1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S1");
    }

    [Test]
    public async Task AssertFilePartIsDownloaded_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        AssertFilePartIsDownloadedRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<AssertFilePartIsDownloadedRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (AssertFilePartIsDownloadedRequest)r)
            .Returns(Task.CompletedTask);
        
        var function = new FileTransferFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new TransferParameters { SharedFileDefinition = new SharedFileDefinition { Id = "F1" } };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.AssertFilePartIsDownloaded(request, context, "S1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S1");
    }

    [Test]
    public async Task AssertUploadIsFinished_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        AssertUploadIsFinishedRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<AssertUploadIsFinishedRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (AssertUploadIsFinishedRequest)r)
            .Returns(Task.CompletedTask);
        
        var function = new FileTransferFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new TransferParameters { SharedFileDefinition = new SharedFileDefinition { Id = "F1" } };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.AssertUploadIsFinished(request, context, "S1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S1");
    }

    [Test]
    public async Task AssertDownloadIsFinished_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();

        AssertDownloadIsFinishedRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<AssertDownloadIsFinishedRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (AssertDownloadIsFinishedRequest)r)
            .Returns(Task.CompletedTask);
        
        var function = new FileTransferFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new TransferParameters { SharedFileDefinition = new SharedFileDefinition { Id = "F1" } };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.AssertDownloadIsFinished(request, context, "S1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S1");
    }
}
