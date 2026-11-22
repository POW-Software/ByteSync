using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Security;

public class TransferAuthorizationSecurityTests
{
    private ICloudSessionsRepository _sessions = null!;
    private ISharedFilesRepository _files = null!;
    private ITrackingActionRepository _actions = null!;
    private TransferAuthorizationService _service = null!;
    private Client _owner = null!;
    private Client _reader = null!;
    private CloudSessionData _session = null!;
    private SharedFileData? _record;

    private static Client Client(char identity) => new() { Identity = new string(identity, 64), AuthenticationEpoch = AuthenticationProtocol.Epoch,
        ClientInstanceId = new string(identity, 64) + "_" + Guid.NewGuid().ToString("D") };

    [SetUp]
    public void SetUp()
    {
        _owner = Client('A');
        _reader = Client('B');
        _sessions = A.Fake<ICloudSessionsRepository>();
        _files = A.Fake<ISharedFilesRepository>();
        _actions = A.Fake<ITrackingActionRepository>();
        _record = null;
        _session = new CloudSessionData { AuthenticationEpoch = AuthenticationProtocol.Epoch, SessionId = "session", IsSessionActivated = true };
        foreach (var client in new[] { _owner, _reader })
            _session.SessionMembers.Add(new SessionMemberData { ClientInstanceId = client.ClientInstanceId, Identity = client.Identity });
        A.CallTo(() => _sessions.Get("session")).Returns(_session);
        A.CallTo(() => _files.AddOrUpdate(A<SharedFileDefinition>._, A<Func<SharedFileData?, SharedFileData>>._))
            .Invokes((SharedFileDefinition definition, Func<SharedFileData?, SharedFileData> update) => _record = update(_record));
        A.CallTo(() => _files.Get(A<string>._)).ReturnsLazily(() => _record);
        _service = new TransferAuthorizationService(_sessions, _files, _actions, Options.Create(new AppSettings { DefaultStorageProvider = StorageProvider.AzureBlobStorage }));
    }

    private TransferParameters Parameters() => new()
    {
        SessionId = "session", PartNumber = 1, SharedFileDefinition = new SharedFileDefinition { Id = "file", SessionId = "session",
            ClientInstanceId = _owner.ClientInstanceId, SharedFileType = SharedFileTypes.FullInventory, AdditionalName = "inventory", IV = new byte[16], StorageObjectId = "attacker-selected-object" }
    };

    [Test]
    public async Task Upload_CreatesImmutableServerOwnedObjectAndAllowsRecipientDownload()
    {
        var upload = Parameters();

        await _service.Authorize("session", _owner, upload, true, true);
        _record!.UploadedPartsNumbers.Add(1);
        var download = Parameters();
        await _service.Authorize("session", _reader, download, false, false);

        _record.SharedFileDefinition.StorageObjectId.Should().NotBe("attacker-selected-object");
        Guid.TryParseExact(_record.SharedFileDefinition.StorageObjectId, "N", out _).Should().BeTrue();
        download.SharedFileDefinition.Should().BeSameAs(_record.SharedFileDefinition);
        download.StorageProvider.Should().Be(StorageProvider.AzureBlobStorage);
    }

    [TestCase("route")]
    [TestCase("body")]
    [TestCase("owner")]
    [TestCase("part")]
    public async Task Transfer_WithInconsistentBindings_HasNoRegistrationSideEffect(string mismatch)
    {
        var parameters = Parameters();
        if (mismatch == "route") parameters.SharedFileDefinition.SessionId = "other-session";
        if (mismatch == "body") parameters.SessionId = "other-session";
        if (mismatch == "owner") parameters.SharedFileDefinition.ClientInstanceId = _reader.ClientInstanceId;
        if (mismatch == "part") parameters.PartNumber = -1;

        var act = () => _service.Authorize("session", _owner, parameters, true, true);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _record.Should().BeNull();
        A.CallTo(() => _files.AddOrUpdate(A<SharedFileDefinition>._, A<Func<SharedFileData?, SharedFileData>>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task RegisteredObject_CannotChangeItsMetadataOrSession()
    {
        await _service.Authorize("session", _owner, Parameters(), true, true);
        var altered = Parameters();
        altered.SharedFileDefinition.AdditionalName = "../other-object";

        var act = () => _service.Authorize("session", _owner, altered, true, true);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _record!.SharedFileDefinition.AdditionalName.Should().Be("inventory");
    }

    [Test]
    public async Task Download_RequiresRegisteredUploadedPartAndExplicitRecipient()
    {
        var parameters = Parameters();
        var missing = () => _service.Authorize("session", _reader, parameters, false, false);
        await missing.Should().ThrowAsync<UnauthorizedAccessException>();
        await _service.Authorize("session", _owner, parameters, true, true);
        var notUploaded = () => _service.Authorize("session", _reader, Parameters(), false, false);
        await notUploaded.Should().ThrowAsync<UnauthorizedAccessException>();
        _record!.UploadedPartsNumbers.Add(1);
        _record.Recipients.Clear();
        var notRecipient = () => _service.Authorize("session", _reader, Parameters(), false, false);
        await notRecipient.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task SynchronizationUpload_RequiresOwnershipOfEveryReferencedAction()
    {
        var parameters = Parameters();
        parameters.SharedFileDefinition.SharedFileType = SharedFileTypes.FullSynchronization;
        parameters.SharedFileDefinition.ActionsGroupIds = new List<string> { "action" };
        A.CallTo(() => _actions.GetOrThrow("session", "action")).Returns(new TrackingActionEntity { SourceClientInstanceId = _reader.ClientInstanceId });

        var act = () => _service.Authorize("session", _owner, parameters, true, true);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _record.Should().BeNull();
    }

    [Test]
    public async Task FinishingUpload_CannotHidePreviouslyUploadedParts()
    {
        await _service.Authorize("session", _owner, Parameters(), true, true);
        _record!.UploadedPartsNumbers.Add(3);
        var parameters = Parameters();
        parameters.PartNumber = null;
        parameters.TotalParts = 2;

        var act = () => _service.Authorize("session", _owner, parameters, true, false);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
    [Test]
    public async Task CompletedPart_KeepsServerRecordForAuthorizedFinalAcknowledgement()
    {
        await _service.Authorize("session", _owner, Parameters(), true, true);
        _record!.UploadedPartsNumbers.Add(1);
        _record.TotalParts = 1;
        var parameters = Parameters();
        await _service.Authorize("session", _reader, parameters, false, false);
        var sharedFiles = new SharedFilesService(_files,
            A.Fake<ByteSync.ServerCommon.Interfaces.Services.Storage.IAzureBlobStorageService>(),
            A.Fake<ByteSync.ServerCommon.Interfaces.Services.Storage.ICloudflareR2Service>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SharedFilesService>.Instance,
            Options.Create(new AppSettings()));

        await sharedFiles.AssertFilePartIsDownloaded(_reader, parameters);
        parameters.PartNumber = null;
        await _service.Authorize("session", _reader, parameters, false, false);

        _record.IsFullyDownloaded.Should().BeTrue();
        A.CallTo(() => _files.Forget(A<SharedFileDefinition>._)).MustNotHaveHappened();
    }

}
