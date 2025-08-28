using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;

namespace ByteSync.Business.Actions.Shared;

public class SharedActionsGroup : AbstractActionsGroup
{
    public SharedActionsGroup()
    {
        Targets = new HashSet<SharedDataPart>();
    }

    public PathIdentity PathIdentity { get; set; } = null!;

    public SharedDataPart? Source { get; set; }

    public HashSet<SharedDataPart> Targets { get; set; }

    public SynchronizationTypes? SynchronizationType { get; set; }
        
    public SynchronizationStatus? SynchronizationStatus { get; set; }
        
    public bool IsFromSynchronizationRule { get; set; }

    public string LinkingKeyValue
    {
        get
        {
            return PathIdentity.LinkingKeyValue;
        }
    }

    public bool IsFile
    {
        get
        {
            return PathIdentity.FileSystemType == FileSystemTypes.File;
        }
    }
        
    public bool IsDirectory
    {
        get
        {
            return PathIdentity.FileSystemType == FileSystemTypes.Directory;
        }
    }

    public string Key
    {
        get
        {
            return Source?.ClientInstanceId
                   + "___"
                   + Targets
                       .Select(c => c.ClientInstanceId)
                       .OrderBy(s => s).ToList()
                       .JoinToString("_")
                   + "___"
                   + SynchronizationType;
        }
    }

    public string GetSourceFullName()
    {
        string sourceFileName = GetFullName(Source!);

        return sourceFileName;
    }

    public HashSet<string> GetTargetsFullNames(ByteSyncEndpoint endpoint)
    {
        HashSet<string> result = new HashSet<string>();

        foreach (var target in Targets.Where(sdp => Equals(sdp.ClientInstanceId, endpoint.ClientInstanceId)))
        {
            var fullName = GetFullName(target);
            result.Add(fullName);
        }

        return result;
    }
        
    public string GetFullName(SharedDataPart sharedDataPart)
    {
        string sourceFileName;
        if (sharedDataPart.InventoryPartType == FileSystemTypes.Directory)
        {
            sourceFileName = IOUtils.Combine(sharedDataPart.RootPath, sharedDataPart.RelativePath!);
        }
        else
        {
            sourceFileName = sharedDataPart.RootPath;
        }

        return sourceFileName;
    }
        
    private bool Equals(SharedActionsGroup other)
    {
        return ActionsGroupId == other.ActionsGroupId;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SharedActionsGroup) obj);
    }

    public override int GetHashCode()
    {
        return ActionsGroupId.GetHashCode();
    }

    public ActionsGroupDefinition GetDefinition()
    {
        var actionsGroupDefinition = new ActionsGroupDefinition();

        actionsGroupDefinition.ActionsGroupId = ActionsGroupId;
        actionsGroupDefinition.FileSystemType = PathIdentity.FileSystemType;
        actionsGroupDefinition.Operator = Operator;
        actionsGroupDefinition.AppliesOnlySynchronizeDate = AppliesOnlySynchronizeDate;
        actionsGroupDefinition.SourceClientInstanceId = Source?.ClientInstanceId;
        actionsGroupDefinition.TargetClientInstanceAndNodeIds = Targets.Select(t => $"{t.ClientInstanceId}_{t.NodeId}").ToList();
        actionsGroupDefinition.Size = Size;
        actionsGroupDefinition.CreationTimeUtc = CreationTimeUtc;
        actionsGroupDefinition.LastWriteTimeUtc = LastWriteTimeUtc;

        return actionsGroupDefinition;
    }
}