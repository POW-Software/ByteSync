using System.IO;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.DataSources;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Synchronizations;

namespace ByteSync.Services.Inventories;

public class LocalSourceAuthorizationService(IApplicationSettingsRepository settings, IConnectionService connection,
    ISessionService session, ByteSync.Interfaces.Repositories.IDataNodeRepository nodes) : ILocalSourceAuthorizationService
{
    private sealed record GrantData(object? Session, string SessionId, string Id, string NodeId, string InstanceId, string Root, FileSystemTypes Type);
    private readonly Dictionary<(string, string), GrantData> _grants = new();
    private readonly object _gate = new();

    public void Grant(DataSource source, bool restoreOnly = false)
    {
        if (source.ClientInstanceId != connection.ClientInstanceId || session.SessionId == null ||
            string.IsNullOrEmpty(source.Id) || string.IsNullOrEmpty(source.DataNodeId)) throw new UnauthorizedAccessException();
        if (source.Type is not (FileSystemTypes.File or FileSystemTypes.Directory)) throw new UnauthorizedAccessException();
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source.Path));
        if (!Path.IsPathFullyQualified(source.Path)) throw new UnauthorizedAccessException();
        LocalPathSafety.AssertNoLinks(root);
        var identity = ByteSync.Common.Business.Auth.AuthenticationProtocol.GetIdentity(settings.GetCurrentApplicationSettings().DecodedRsaPublicKey!);
        var permission = new LocalSourcePermission(identity, root, source.Type);
        lock (_gate)
        {
            var permissions = settings.GetCurrentApplicationSettings().LocalSourcePermissions;
            if (restoreOnly && !permissions.Contains(permission))
                throw new UnauthorizedAccessException("Select the profile source locally before running it automatically");
            if (!restoreOnly && !permissions.Contains(permission))
                settings.UpdateCurrentApplicationSettings(s => s.LocalSourcePermissions.Add(permission));
            _grants[(session.SessionId, source.Id)] = new GrantData(session.CurrentSession, session.SessionId, source.Id, source.DataNodeId,
                source.ClientInstanceId, root, source.Type);
        }
    }

    public void Revoke(DataSource source)
    {
        lock (_gate)
        {
            if (session.SessionId != null) _grants.Remove((session.SessionId, source.Id));
            settings.UpdateCurrentApplicationSettings(s => s.LocalSourcePermissions.RemoveAll(p =>
                p.Path == Path.TrimEndingDirectorySeparator(Path.GetFullPath(source.Path)) && p.Type == source.Type));
        }
    }

    public void CheckNetworkSource(DataSource source, string sender)
    {
        if (source.ClientInstanceId != sender) throw new UnauthorizedAccessException("Source owner mismatch");
        var node = nodes.GetElement(source.DataNodeId);
        if (node == null || node.ClientInstanceId != sender) throw new UnauthorizedAccessException("Source node owner mismatch");
        if (sender != connection.ClientInstanceId) return;
        var part = new SharedDataPart { SourceId = source.Id, NodeId = source.DataNodeId, ClientInstanceId = sender,
            InventoryPartType = source.Type, SourceRelativePath = "" };
        var expected = Resolve(part);
        if (!string.Equals(expected, Path.TrimEndingDirectorySeparator(Path.GetFullPath(source.Path)), LocalPathSafety.Comparison))
            throw new UnauthorizedAccessException("Network data cannot change a local source root");
    }

    public string Resolve(SharedDataPart part)
    {
        lock (_gate)
        {
            if (session.SessionId == null || part.SourceId == null ||
                !_grants.TryGetValue((session.SessionId, part.SourceId), out var grant) ||
                part.ClientInstanceId != connection.ClientInstanceId || grant.InstanceId != part.ClientInstanceId ||
                !ReferenceEquals(grant.Session, session.CurrentSession) || grant.NodeId != part.NodeId || grant.Type != part.InventoryPartType) throw new UnauthorizedAccessException("Local source is not authorized");
            LocalPathSafety.ValidateRelativePath(part.SourceRelativePath);
            if (grant.Type == FileSystemTypes.File && part.SourceRelativePath != "") throw new UnauthorizedAccessException("File source cannot authorize siblings");
            var resolved = Path.GetFullPath(Path.Combine(grant.Root, part.SourceRelativePath!.Replace('/', Path.DirectorySeparatorChar)));
            var boundary = Path.EndsInDirectorySeparator(grant.Root) ? grant.Root : grant.Root + Path.DirectorySeparatorChar;
            if (!string.Equals(resolved, grant.Root, LocalPathSafety.Comparison) &&
                !resolved.StartsWith(boundary, LocalPathSafety.Comparison))
                throw new UnauthorizedAccessException("Path escapes source");
            LocalPathSafety.AssertNoLinks(resolved);
            return resolved;
        }
    }

    public void ValidatePlan(IEnumerable<SharedActionsGroup> groups)
    {
        var bindings = new List<SharedDataPart>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            if (group == null || string.IsNullOrEmpty(group.ActionsGroupId) || !ids.Add(group.ActionsGroupId))
                throw new UnauthorizedAccessException("Invalid or duplicate action id");
            LocalPathSafety.ValidateComponent(group.ActionsGroupId);
            if (!Enum.IsDefined(group.Operator) || group.Targets == null || group.PathIdentity == null ||
                (group.NeedsOperatingOnSourceAndTargets && group.Source == null)) throw new UnauthorizedAccessException("Invalid synchronization plan");
            if (group.PathIdentity.FileSystemType is not (FileSystemTypes.File or FileSystemTypes.Directory) ||
                (group.IsCreate && !group.IsDirectory) || ((group.IsCopyContent || group.IsCopyDates) && !group.IsFile) ||
                ((group.IsFullCopy || group.IsCopyDates) && (!group.CreationTimeUtc.HasValue || !group.LastWriteTimeUtc.HasValue)) ||
                (group.IsCopyContent && group.SynchronizationType is not (ByteSync.Common.Business.Actions.SynchronizationTypes.Full or ByteSync.Common.Business.Actions.SynchronizationTypes.Delta)))
                throw new UnauthorizedAccessException("Incomplete synchronization operation");
            foreach (var part in group.Targets.Concat(group.Source == null ? Array.Empty<SharedDataPart>() : new[] { group.Source }))
            {
                if (part == null || string.IsNullOrEmpty(part.SourceId) || string.IsNullOrEmpty(part.NodeId) || string.IsNullOrEmpty(part.ClientInstanceId))
                    throw new UnauthorizedAccessException("Missing action source binding");
                LocalPathSafety.ValidateRelativePath(part.SourceRelativePath);
            }
            if (group.Source != null && group.Source.ClientInstanceId == connection.ClientInstanceId) bindings.Add(group.Source);
            bindings.AddRange(group.Targets.Where(p => p.ClientInstanceId == connection.ClientInstanceId));
        }
        foreach (var part in bindings) Resolve(part);
        foreach (var part in bindings) part.LocalPathResolver = () => Resolve(part);
    }

    public void ValidatePath(string path)
    {
        lock (_gate)
        {
            var full = Path.GetFullPath(path);
            foreach (var grant in _grants.Values.Where(g => g.SessionId == session.SessionId))
            {
                var relative = Path.GetRelativePath(grant.Root, full).Replace(Path.DirectorySeparatorChar, '/');
                if (relative == ".") relative = "";
                try
                {
                    var resolved = Resolve(new SharedDataPart { SourceId = grant.Id, NodeId = grant.NodeId, ClientInstanceId = grant.InstanceId,
                        InventoryPartType = grant.Type, SourceRelativePath = relative });
                    if (string.Equals(resolved, full, StringComparison.Ordinal)) return;
                }
                catch (UnauthorizedAccessException) { }
            }
            throw new UnauthorizedAccessException("Path has no current local authorization");
        }
    }

    public void ValidateDestination(SharedActionsGroup group, string destination)
    {
        if (!group.Targets.Where(p => p.ClientInstanceId == connection.ClientInstanceId)
                .Any(p => string.Equals(Resolve(p), destination, LocalPathSafety.Comparison)))
            throw new UnauthorizedAccessException("Destination is not part of the authorized action");
    }
}
