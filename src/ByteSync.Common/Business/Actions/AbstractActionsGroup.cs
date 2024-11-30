using System;

namespace ByteSync.Common.Business.Actions;

public class AbstractActionsGroup : AbstractAction
{
    public string ActionsGroupId { get; set; } = null!;
    
    public long? Size { get; set; }

    public DateTime? CreationTimeUtc { get; set; }
    
    public DateTime? LastWriteTimeUtc { get; set; }
    
    public bool AppliesOnlySynchronizeDate { get; set; }
    
    public bool IsFinallySynchronizeContentAndDate
    {
        get
        {
            return IsSynchronizeContentAndDate && !AppliesOnlySynchronizeDate;
        }
    }
    
    public bool IsFinallySynchronizeDate
    {
        get
        {
            return IsSynchronizeContentAndDate && AppliesOnlySynchronizeDate;
        }
    }

    public bool IsInitialOperatingOnSourceNeeded
    {
        get
        {
            return IsSynchronizeContentOnly || IsFinallySynchronizeContentAndDate;
        }
    }
    
    public bool NeedsOperatingOnSourceAndTargets
    {
        get
        {
            return !NeedsOnlyOperatingOnTargets;
        }
    }
    
    public bool NeedsOnlyOperatingOnTargets
    {
        get
        {
            return IsSynchronizeDate || IsCreate || IsDelete || IsFinallySynchronizeDate;
        }
    }
}