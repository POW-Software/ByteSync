using System;

namespace ByteSync.Common.Business.Actions;

public class AbstractActionsGroup : AbstractAction
{
    public string ActionsGroupId { get; set; } = null!;
    
    public long? Size { get; set; }
    
    public DateTime? CreationTimeUtc { get; set; }
    
    public DateTime? LastWriteTimeUtc { get; set; }
    
    public bool AppliesOnlyCopyDate { get; set; }
    
    public bool IsFinallyCopyContentAndDate
    {
        get { return IsFullCopy && !AppliesOnlyCopyDate; }
    }
    
    public bool IsFinallySynchronizeDate
    {
        get { return IsFullCopy && AppliesOnlyCopyDate; }
    }
    
    public bool IsInitialOperatingOnSourceNeeded
    {
        get { return IsCopyContentOnly || IsFinallyCopyContentAndDate; }
    }
    
    public bool NeedsOperatingOnSourceAndTargets
    {
        get { return !NeedsOnlyOperatingOnTargets; }
    }
    
    public bool NeedsOnlyOperatingOnTargets
    {
        get { return IsCopyDates || IsCreate || IsDelete || IsFinallySynchronizeDate; }
    }
}