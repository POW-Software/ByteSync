using ByteSync.Common.Helpers;

namespace ByteSync.Common.Business.Actions;

public abstract class AbstractAction
{
    public ActionOperatorTypes Operator { get; set; }
    
    public bool IsCopyContent
    {
        get { return Operator.In(ActionOperatorTypes.CopyContentOnly, ActionOperatorTypes.Copy); }
    }
    
    public bool IsCopyContentOnly
    {
        get { return Operator.In(ActionOperatorTypes.CopyContentOnly); }
    }
    
    public bool IsFullCopy
    {
        get { return Operator.In(ActionOperatorTypes.Copy); }
    }
    
    public bool IsCopyDates
    {
        get { return Operator.In(ActionOperatorTypes.CopyDatesOnly); }
    }
    
    public bool IsDelete
    {
        get { return Operator.In(ActionOperatorTypes.Delete); }
    }
    
    public bool IsCreate
    {
        get { return Operator.In(ActionOperatorTypes.Create); }
    }
    
    public bool IsDoNothing
    {
        get { return Operator.In(ActionOperatorTypes.DoNothing); }
    }
}