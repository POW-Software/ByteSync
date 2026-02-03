using System.Reflection;
using System.Runtime.Serialization;
using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ByteSync.ServerCommon.Interfaces.Services.Storage.Factories;
using ByteSync.ServerCommon.Services.Storage;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Services;

[TestFixture]
public class AzureBlobStorageServiceTests
{
    private IAzureBlobContainerClientFactory _clientFactory = null!;
    private ILogger<AzureBlobStorageService> _logger = null!;
    private AzureBlobStorageService _service = null!;
    
    [SetUp]
    public void SetUp()
    {
        _clientFactory = A.Fake<IAzureBlobContainerClientFactory>();
        _logger = A.Fake<ILogger<AzureBlobStorageService>>();
        _service = new AzureBlobStorageService(_clientFactory, _logger);
    }
    
    [Test]
    public async Task GetAllObjects_ShouldReturnBlobsAndCreatedOn()
    {
        var createdOn1 = DateTimeOffset.UtcNow.AddMinutes(-10);
        var createdOn2 = DateTimeOffset.UtcNow.AddMinutes(-5);
        var item1 = CreateBlobItem("file-a", createdOn1);
        var item2 = CreateBlobItem("file-b", createdOn2);
        
        var pageable = BuildPageable(item1, item2);
        var container = new TestBlobContainerClient(pageable);
        
        A.CallTo(() => _clientFactory.GetOrCreateContainer(A<CancellationToken>._))
            .Returns(container);
        
        var results = await _service.GetAllObjects(CancellationToken.None);
        
        results.Should().HaveCount(2);
        results.Should().Contain(new KeyValuePair<string, DateTimeOffset?>("file-a", createdOn1));
        results.Should().Contain(new KeyValuePair<string, DateTimeOffset?>("file-b", createdOn2));
        container.LastOptions.Should().NotBeNull();
        container.LastOptions!.Traits.Should().Be(BlobTraits.Metadata);
        container.LastOptions.States.Should().Be(BlobStates.All);
    }
    
    [Test]
    public async Task GetAllObjects_WhenNoBlobs_ShouldReturnEmptyList()
    {
        var pageable = BuildPageable();
        var container = new TestBlobContainerClient(pageable);
        
        A.CallTo(() => _clientFactory.GetOrCreateContainer(A<CancellationToken>._))
            .Returns(container);
        
        var results = await _service.GetAllObjects(CancellationToken.None);
        
        results.Should().BeEmpty();
        container.LastOptions.Should().NotBeNull();
        container.LastOptions!.Traits.Should().Be(BlobTraits.Metadata);
        container.LastOptions.States.Should().Be(BlobStates.All);
    }
    
    private static AsyncPageable<BlobItem> BuildPageable(params BlobItem[] items)
    {
        var page = Page<BlobItem>.FromValues(items, continuationToken: null, response: new TestResponse(200));
        
        return AsyncPageable<BlobItem>.FromPages([page]);
    }
    
    private static BlobItem CreateBlobItem(string name, DateTimeOffset? createdOn)
    {
#pragma warning disable SYSLIB0050
        var properties = (BlobItemProperties)FormatterServices
            .GetUninitializedObject(typeof(BlobItemProperties));
#pragma warning restore SYSLIB0050
        var createdOnProperty = typeof(BlobItemProperties).GetProperty(nameof(BlobItemProperties.CreatedOn));
        createdOnProperty!.SetValue(properties, createdOn,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, null, null);
        
        return BlobsModelFactory.BlobItem(name, false, properties, null, null);
    }
    
    private sealed class TestBlobContainerClient : BlobContainerClient
    {
        private readonly AsyncPageable<BlobItem> _pageable;
        
        public TestBlobContainerClient(AsyncPageable<BlobItem> pageable)
            : base(new Uri("https://example.com/container"))
        {
            _pageable = pageable;
        }
        
        public GetBlobsOptions? LastOptions { get; private set; }
        
        public override AsyncPageable<BlobItem> GetBlobsAsync(GetBlobsOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            LastOptions = options;
            
            return _pageable;
        }
    }
    
    private sealed class TestResponse : Response
    {
        private readonly int _status;
        
        public TestResponse(int status)
        {
            _status = status;
        }
        
        public override int Status => _status;
        
        public override string ReasonPhrase => string.Empty;
        
        public override Stream? ContentStream { get; set; }
        
        public override string ClientRequestId { get; set; } = string.Empty;
        
        public override void Dispose()
        {
        }
        
        protected override bool TryGetHeader(string name, out string value)
        {
            value = string.Empty;
            
            return false;
        }
        
        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            values = [];
            
            return false;
        }
        
        protected override bool ContainsHeader(string name)
        {
            return false;
        }
        
        protected override IEnumerable<HttpHeader> EnumerateHeaders()
        {
            return [];
        }
    }
}