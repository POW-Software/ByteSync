using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Configurations;
using ByteSync.Business.DataSources;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Inventories;
using ByteSync.Services.Synchronizations;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Security;

public class LocalSourceAuthorizationSecurityTests
{
    private string _directory = null!;
    private ApplicationSettings _settings = null!;
    private LocalSourceAuthorizationService _service = null!;
    private DataSource _source = null!;
    private const string Instance = "local-instance";

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "ByteSyncSecurityTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _settings = new ApplicationSettings();
        _settings.SetEncryptionPassword("local-test-only-password");
        _settings.InstallationId = "test";
        _settings.InitializeRsa();
        var settings = new Mock<IApplicationSettingsRepository>();
        settings.Setup(s => s.GetCurrentApplicationSettings()).Returns(_settings);
        settings.Setup(s => s.UpdateCurrentApplicationSettings(It.IsAny<Action<ApplicationSettings>>(), It.IsAny<bool>()))
            .Returns((Action<ApplicationSettings> update, bool save) => { update(_settings); return _settings; });
        var connection = new Mock<IConnectionService>();
        connection.SetupGet(c => c.ClientInstanceId).Returns(Instance);
        var session = new Mock<ISessionService>();
        session.SetupGet(s => s.SessionId).Returns("session");
        var nodes = new Mock<ByteSync.Interfaces.Repositories.IDataNodeRepository>();
        nodes.Setup(n => n.GetElement("node")).Returns(new ByteSync.Business.DataNodes.DataNode { Id = "node", ClientInstanceId = Instance });
        _service = new LocalSourceAuthorizationService(settings.Object, connection.Object, session.Object, nodes.Object);
        _source = new DataSource { Id = "source", DataNodeId = "node", ClientInstanceId = Instance, Path = _directory, Type = FileSystemTypes.Directory };
        _service.Grant(_source);
    }

    [TearDown]
    public void TearDown()
    {
        _settings.PrivateRsa.Dispose();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }

    private SharedDataPart Part(string relative = "file.txt") => new()
    {
        SourceId = _source.Id, NodeId = _source.DataNodeId, ClientInstanceId = Instance,
        InventoryPartType = _source.Type, RootPath = "untrusted-root-from-peer", SourceRelativePath = relative,
        RelativePath = "/" + relative
    };

    private static SharedActionsGroup Group(SharedDataPart part) => new()
    {
        ActionsGroupId = Guid.NewGuid().ToString(), Operator = ActionOperatorTypes.Delete,
        PathIdentity = new PathIdentity(FileSystemTypes.File, "file", "file", "file"), Targets = new HashSet<SharedDataPart> { part }
    };

    [TestCase("../outside")]
    [TestCase("a/../../outside")]
    [TestCase("/etc/passwd")]
    [TestCase("C:/Windows/file")]
    [TestCase("C:relative")]
    [TestCase(@"\\server\share\file")]
    [TestCase(@"a\..\outside")]
    [TestCase("a//file")]
    [TestCase("./file")]
    [TestCase("a/./file")]
    public void PeerRelativePaths_CannotEscapeOrUseAmbiguousForms(string relative)
    {
        var act = () => _service.Resolve(Part(relative));

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Test]
    public void PeerRoot_IsIgnoredAndOnlyLocalGrantResolvesPaths()
    {
        var part = Part("sub/file.txt");
        var expected = Path.Combine(_directory, "sub", "file.txt");

        _service.Resolve(part).Should().Be(expected);
        _service.ValidatePlan(new[] { Group(part) });
        part.LocalPathResolver!().Should().Be(expected);
    }

    [TestCase("owner")]
    [TestCase("node")]
    [TestCase("source")]
    [TestCase("type")]
    public void PeerBinding_MustMatchTheLocalGrant(string field)
    {
        var part = Part();
        switch (field)
        {
            case "owner": part.ClientInstanceId = "remote"; break;
            case "node": part.NodeId = "other-node"; break;
            case "source": part.SourceId = "other-source"; break;
            case "type": part.InventoryPartType = FileSystemTypes.File; break;
        }

        var act = () => _service.Resolve(part);

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Test]
    public void FileGrant_AuthorizesOnlyTheSelectedFile()
    {
        _service.Revoke(_source);
        _source.Path = Path.Combine(_directory, "only.txt");
        _source.Type = FileSystemTypes.File;
        File.WriteAllText(_source.Path, "authorized");
        _service.Grant(_source);

        _service.Resolve(Part("")).Should().Be(_source.Path);
        var sibling = () => _service.Resolve(Part("sibling.txt"));
        var siblingAccess = () => _service.ValidatePath(Path.Combine(_directory, "sibling.txt"));

        sibling.Should().Throw<UnauthorizedAccessException>();
        siblingAccess.Should().Throw<UnauthorizedAccessException>();
    }

    [Test]
    public void NetworkSources_CannotClaimLocalOwnershipOrChangeTheRoot()
    {
        var remote = new DataSource { Id = "remote", ClientInstanceId = Instance, DataNodeId = "node", Path = _directory, Type = FileSystemTypes.Directory };
        var spoof = () => _service.CheckNetworkSource(remote, "remote-instance");
        spoof.Should().Throw<UnauthorizedAccessException>();
        var echo = new DataSource { Id = _source.Id, ClientInstanceId = Instance, DataNodeId = "node", Path = _directory + "-sibling", Type = FileSystemTypes.Directory };
        var changeRoot = () => _service.CheckNetworkSource(echo, Instance);
        changeRoot.Should().Throw<UnauthorizedAccessException>();
        _service.CheckNetworkSource(_source, Instance);
    }

    [Test]
    public void RevokedGrant_InvalidatesPreviouslyPreparedActions()
    {
        var part = Part();
        var group = Group(part);
        _service.ValidatePlan(new[] { group });
        _service.Revoke(_source);

        var act = () => group.GetFullName(part);

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Test]
    public void Plan_WithValidThenInvalidAction_BindsNothingAndMutatesNothing()
    {
        var sentinel = Path.Combine(_directory, "file.txt");
        File.WriteAllText(sentinel, "unchanged");
        var valid = Part();
        var groups = new[] { Group(valid), Group(Part("../outside")) };

        var act = () => _service.ValidatePlan(groups);

        act.Should().Throw<UnauthorizedAccessException>();
        valid.LocalPathResolver.Should().BeNull();
        File.ReadAllText(sentinel).Should().Be("unchanged");
    }

    [Test]
    public void ProfileRestore_RequiresAnExistingIndependentLocalPermission()
    {
        var another = new DataSource { Id = "restored", ClientInstanceId = Instance, DataNodeId = "node", Path = _directory, Type = FileSystemTypes.Directory };
        _service.Grant(another, true);
        another.Path = _directory + "-untrusted";

        var restore = () => _service.Grant(another, true);

        restore.Should().Throw<UnauthorizedAccessException>();
        _settings.LocalSourcePermissions.Should().HaveCount(1);
    }

    [Test]
    public void LocalPermissions_SurviveXmlWithoutChangingTheRsaKeys()
    {
        var serializer = new XmlSerializer(typeof(ApplicationSettings));
        using var writer = new StringWriter();
        serializer.Serialize(writer, _settings);
        using var reader = new StringReader(writer.ToString());
        var restored = (ApplicationSettings)serializer.Deserialize(reader)!;
        restored.SetEncryptionPassword("local-test-only-password");

        restored.LocalSourcePermissions.Should().BeEquivalentTo(_settings.LocalSourcePermissions);
        restored.DecodedRsaPublicKey.Should().Equal(_settings.DecodedRsaPublicKey!);
        restored.PrivateRsa.Dispose();
    }

    [Test]
    public void LinkInsertedAfterPreparation_IsRejectedBeforeAccess()
    {
        var sub = Path.Combine(_directory, "sub");
        var outside = Path.Combine(_directory, "outside");
        Directory.CreateDirectory(sub);
        Directory.CreateDirectory(outside);
        var part = Part("sub/file.txt");
        _service.ValidatePlan(new[] { Group(part) });
        Directory.Delete(sub);
        try { Directory.CreateSymbolicLink(sub, outside); }
        catch (UnauthorizedAccessException) { Assert.Ignore("Creating symbolic links requires Windows Developer Mode or a symlink privilege"); }
        try
        {
            var act = () => part.LocalPathResolver!();
            act.Should().Throw<UnauthorizedAccessException>();
        }
        finally { Directory.Delete(sub); }
    }

    [Test]
    public void NativeFileOperations_PreserveLegitimateCopyReplaceAndMetadata()
    {
        var source = Path.Combine(_directory, "source.txt");
        var target = Path.Combine(_directory, "nested", "target.txt");
        File.WriteAllText(source, "contents");
        LocalPathSafety.CreateDirectory(Path.GetDirectoryName(target)!);
        using (var input = LocalPathSafety.OpenRead(source))
        using (var output = LocalPathSafety.OpenWrite(target, true)) input.CopyTo(output);
        var date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        LocalPathSafety.SetDates(target, date, date);

        File.ReadAllText(target).Should().Be("contents");
        File.GetLastWriteTimeUtc(target).Should().Be(date);
        LocalPathSafety.Delete(target);
        File.Exists(target).Should().BeFalse();
    }

    [Test]
    public void WindowsDirectoryLease_PreventsParentSubstitutionDuringAccess()
    {
        if (!OperatingSystem.IsWindows()) Assert.Ignore("Windows sharing semantics test");
        var sub = Path.Combine(_directory, "sub");
        Directory.CreateDirectory(sub);
        using var lease = LocalPathSafety.Acquire(Path.Combine(sub, "file.txt"));

        var swap = () => Directory.Move(sub, sub + "-moved");

        swap.Should().Throw<IOException>();
    }
    [TestCase(ActionOperatorTypes.CopyContentOnly)]
    [TestCase(ActionOperatorTypes.Copy)]
    [TestCase(ActionOperatorTypes.CopyDatesOnly)]
    [TestCase(ActionOperatorTypes.Create)]
    [TestCase(ActionOperatorTypes.Delete)]
    public async System.Threading.Tasks.Task EveryFileOperation_RejectsAnUnauthorizedTargetBeforeMutation(ActionOperatorTypes operation)
    {
        var part = Part("../outside.txt");
        var group = Group(part);
        group.Operator = operation;
        group.Source = Part("source.txt");
        group.SynchronizationType = SynchronizationTypes.Full;
        group.CreationTimeUtc = group.LastWriteTimeUtc = DateTime.UtcNow;
        if (operation == ActionOperatorTypes.Create) group.PathIdentity = new PathIdentity(FileSystemTypes.Directory, "dir", "dir", "dir");
        var handler = Handler();

        var act = () => handler.RunSynchronizationAction(group);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        Directory.GetFiles(_directory).Should().BeEmpty();
    }

    private SynchronizationActionHandler Handler()
    {
        var connection = new Mock<IConnectionService>();
        connection.SetupGet(c => c.CurrentEndPoint).Returns(new ByteSync.Common.Business.EndPoints.ByteSyncEndpoint { ClientInstanceId = Instance });
        return new SynchronizationActionHandler(Mock.Of<ISessionService>(), connection.Object,
            Mock.Of<ByteSync.Interfaces.Controls.Synchronizations.IDeltaManager>(),
            Mock.Of<ByteSync.Interfaces.Controls.Synchronizations.ISynchronizationActionServerInformer>(),
            Mock.Of<ByteSync.Interfaces.Controls.Synchronizations.ISynchronizationActionRemoteUploader>(),
            Mock.Of<ByteSync.Interfaces.Controls.Synchronizations.ISynchronizationService>(),
            Mock.Of<ByteSync.Interfaces.Controls.Communications.Http.ISynchronizationApiClient>(),
            new FileDatesSetter(Microsoft.Extensions.Logging.Abstractions.NullLogger<FileDatesSetter>.Instance, _service),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SynchronizationActionHandler>.Instance, _service);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async System.Threading.Tasks.Task LegitimateFullCopy_UsesLocalRootsDespitePeerRootValues(bool automatedProfileRestore)
    {
        var source = Path.Combine(_directory, "source.txt");
        var target = Path.Combine(_directory, "target.txt");
        File.WriteAllText(source, "legitimate synchronization");
        var group = Group(Part("target.txt"));
        group.Operator = ActionOperatorTypes.CopyContentOnly;
        group.Source = Part("source.txt");
        group.SynchronizationType = SynchronizationTypes.Full;

        if (automatedProfileRestore)
        {
            var restoredSource = new DataSource { Id = "restored-source", DataNodeId = _source.DataNodeId,
                ClientInstanceId = Instance, Path = _directory, Type = _source.Type };
            _service.Grant(restoredSource, true);
            group.Source.SourceId = restoredSource.Id;
            foreach (var part in group.Targets) part.SourceId = restoredSource.Id;
        }

        await Handler().RunSynchronizationAction(group);

        File.ReadAllText(target).Should().Be("legitimate synchronization");
        Directory.GetFiles(_directory, "*.bstemp").Should().BeEmpty();
    }

    [Test]
    public async System.Threading.Tasks.Task LegitimateDifferentialApply_UsesAuthorizedBasisAndReplacement()
    {
        var target = Path.Combine(_directory, "basis.txt");
        var newer = Path.Combine(_directory, "new.txt");
        File.WriteAllText(target, new string('a', 16384));
        File.WriteAllText(newer, new string('a', 8192) + new string('b', 8192));
        using var signature = new MemoryStream();
        using (var basis = File.OpenRead(target)) new FastRsync.Signature.SignatureBuilder().Build(basis, new FastRsync.Signature.SignatureWriter(signature));
        signature.Position = 0;
        using var delta = new MemoryStream();
        using (var input = File.OpenRead(newer))
        {
            var builder = new FastRsync.Delta.DeltaBuilder();
            await builder.BuildDeltaAsync(input, new FastRsync.Signature.SignatureReader(signature, builder.ProgressReport),
                new FastRsync.Core.AggregateCopyOperationsDecorator(new FastRsync.Delta.BinaryDeltaWriter(delta)));
        }
        delta.Position = 0;
        var factory = new Mock<ByteSync.Interfaces.Factories.ITemporaryFileManagerFactory>();
        factory.Setup(f => f.Create(target)).Returns(() => new TemporaryFileManager(target, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemporaryFileManager>.Instance));
        var manager = new DeltaManager(Mock.Of<ICloudSessionLocalDataManager>(), factory.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DeltaManager>.Instance, _service);

        await manager.ApplyDelta(target, delta);

        File.ReadAllBytes(target).Should().Equal(File.ReadAllBytes(newer));
    }

    [Test]
    public void HardLinkedFiles_CannotBeReadOrTruncated()
    {
        if (!OperatingSystem.IsWindows()) Assert.Ignore("Windows hard-link API test");
        var original = Path.Combine(_directory, "original.txt");
        var linked = Path.Combine(_directory, "linked.txt");
        File.WriteAllText(original, "unchanged");
        CreateHardLink(linked, original, IntPtr.Zero).Should().BeTrue();

        var read = () => LocalPathSafety.OpenRead(linked).Dispose();
        var write = () => LocalPathSafety.OpenWrite(linked).Dispose();

        read.Should().Throw<UnauthorizedAccessException>();
        write.Should().Throw<UnauthorizedAccessException>();
        File.ReadAllText(original).Should().Be("unchanged");
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true, EntryPoint = "CreateHardLinkW")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool CreateHardLink(string name, string existing, IntPtr security);

    [TestCase("CON")]
    [TestCase("NUL.txt")]
    [TestCase("name.")]
    [TestCase("name ")]
    [TestCase("file:stream")]
    public void WindowsAmbiguousPaths_AreRejected(string path)
    {
        if (!OperatingSystem.IsWindows()) Assert.Ignore("Windows path syntax");
        var act = () => _service.Resolve(Part(path));
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Test]
    public void DirectoryAuthorization_DoesNotAuthorizeAnAdjacentDirectory()
    {
        var outside = () => _service.ValidatePath(Path.Combine(_directory + "-adjacent", "file.txt"));
        outside.Should().Throw<UnauthorizedAccessException>();
    }

    [Test]
    public void Junction_CannotRedirectAnyNativeFileOperation()
    {
        if (!OperatingSystem.IsWindows()) Assert.Ignore("Windows junction semantics");
        var target = Path.Combine(_directory, "real");
        var junction = Path.Combine(_directory, "junction");
        Directory.CreateDirectory(target);
        var sentinel = Path.Combine(target, "file.txt");
        File.WriteAllText(sentinel, "unchanged");
        Directory.CreateDirectory(junction);
        using (var handle = OpenJunction(junction, 0x40000000, 7, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero))
        {
            var substitute = System.Text.Encoding.Unicode.GetBytes(@"\??\" + target);
            var print = System.Text.Encoding.Unicode.GetBytes(target);
            var buffer = new byte[16 + substitute.Length + 2 + print.Length + 2];
            BitConverter.GetBytes(0xA0000003u).CopyTo(buffer, 0);
            BitConverter.GetBytes((ushort)(buffer.Length - 8)).CopyTo(buffer, 4);
            BitConverter.GetBytes((ushort)substitute.Length).CopyTo(buffer, 10);
            BitConverter.GetBytes((ushort)(substitute.Length + 2)).CopyTo(buffer, 12);
            BitConverter.GetBytes((ushort)print.Length).CopyTo(buffer, 14);
            substitute.CopyTo(buffer, 16);
            print.CopyTo(buffer, 18 + substitute.Length);
            SetReparsePoint(handle, 0x900A4, buffer, buffer.Length, IntPtr.Zero, 0, out _, IntPtr.Zero).Should().BeTrue();
        }
        try
        {
            var linkedFile = Path.Combine(junction, "file.txt");
            var read = () => LocalPathSafety.OpenRead(linkedFile).Dispose();
            var write = () => LocalPathSafety.OpenWrite(linkedFile).Dispose();
            var dates = () => LocalPathSafety.SetDates(linkedFile, DateTime.UtcNow, DateTime.UtcNow);
            var delete = () => LocalPathSafety.Delete(linkedFile);
            var create = () => LocalPathSafety.CreateDirectory(Path.Combine(junction, "new-directory"));
            read.Should().Throw<UnauthorizedAccessException>();
            write.Should().Throw<UnauthorizedAccessException>();
            dates.Should().Throw<UnauthorizedAccessException>();
            delete.Should().Throw<UnauthorizedAccessException>();
            create.Should().Throw<UnauthorizedAccessException>();
            File.ReadAllText(sentinel).Should().Be("unchanged");
            Directory.Exists(Path.Combine(target, "new-directory")).Should().BeFalse();
        }
        finally { Directory.Delete(junction); }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true, EntryPoint = "CreateFileW")]
    private static extern Microsoft.Win32.SafeHandles.SafeFileHandle OpenJunction(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, EntryPoint = "DeviceIoControl")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool SetReparsePoint(Microsoft.Win32.SafeHandles.SafeFileHandle handle, uint code, byte[] input, int inputSize, IntPtr output, int outputSize, out int returned, IntPtr overlapped);

    [Test]
    public async System.Threading.Tasks.Task LegitimateDirectoryDatesAndDeleteActions_StayWithinTheGrant()
    {
        var directory = Part("new-directory");
        var create = Group(directory);
        create.Operator = ActionOperatorTypes.Create;
        create.PathIdentity = new PathIdentity(FileSystemTypes.Directory, "dir", "dir", "dir");
        await Handler().RunSynchronizationAction(create);
        Directory.Exists(Path.Combine(_directory, "new-directory")).Should().BeTrue();
        var file = Path.Combine(_directory, "file.txt");
        File.WriteAllText(file, "content");
        var dates = Group(Part());
        dates.Operator = ActionOperatorTypes.CopyDatesOnly;
        dates.CreationTimeUtc = dates.LastWriteTimeUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await Handler().RunSynchronizationAction(dates);
        File.GetLastWriteTimeUtc(file).Should().Be(dates.LastWriteTimeUtc.Value);
        await Handler().RunSynchronizationAction(Group(Part()));
        File.Exists(file).Should().BeFalse();
        create.Operator = ActionOperatorTypes.Delete;
        await Handler().RunSynchronizationAction(create);
        Directory.Exists(Path.Combine(_directory, "new-directory")).Should().BeFalse();
    }

    [TestCase("path")]
    [TestCase("owner")]
    [TestCase("source-id")]
    public void HostileInventory_IsRejectedBeforeAnHonestCreatorCanPropagateIt(string mismatch)
    {
        var inventory = new ByteSync.Models.Inventories.Inventory
        {
            InventoryId = "IID", Code = "A", NodeId = "node", Endpoint = new ByteSync.Common.Business.EndPoints.ByteSyncEndpoint
            { ClientInstanceId = mismatch == "owner" ? Instance : "remote", OSPlatform = ByteSync.Common.Business.Misc.OSPlatforms.Windows }
        };
        var part = new ByteSync.Models.Inventories.InventoryPart(inventory, _directory, FileSystemTypes.Directory)
        { SourceId = mismatch == "source-id" ? null : "remote-source", Code = "a" };
        inventory.Add(part);
        part.FileDescriptions.Add(new ByteSync.Models.FileSystems.FileDescription { InventoryPart = part, RelativePath = mismatch == "path" ? "/../../outside" : "/file.txt" });
        var zip = Path.Combine(_directory, "inventory.zip");
        var saver = new InventorySaver();
        saver.Start(zip);
        saver.WriteInventory(inventory);
        saver.Stop();

        var act = () => { using var loader = new InventoryLoader(zip, "remote"); };

        act.Should().Throw<UnauthorizedAccessException>();
    }

}
