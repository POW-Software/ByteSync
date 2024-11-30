using ByteSync.Common.Helpers;

namespace ByteSync.Common.Business.Actions;

public abstract class AbstractAction
{
    public ActionOperatorTypes Operator { get; set; }
    
    public bool IsSynchronizeContent
    {
        get
        {
            return Operator.In(ActionOperatorTypes.SynchronizeContentOnly, ActionOperatorTypes.SynchronizeContentAndDate);
        }
    }
    
    public bool IsSynchronizeContentOnly
    {
        get
        {
            return Operator.In(ActionOperatorTypes.SynchronizeContentOnly);
        }
    }
    
    public bool IsSynchronizeContentAndDate
    {
        get
        {
            return Operator.In(ActionOperatorTypes.SynchronizeContentAndDate);
        }
    }
    
    public bool IsSynchronizeDate
    {
        get
        {
            return Operator.In(ActionOperatorTypes.SynchronizeDate);
        }
    }
    
    public bool IsDelete
    {
        get
        {
            return Operator.In(ActionOperatorTypes.Delete);
        }
    }
    
    public bool IsCreate
    {
        get
        {
            return Operator.In(ActionOperatorTypes.Create);
        }
    }
    
    public bool IsDoNothing
    {
        get
        {
            return Operator.In(ActionOperatorTypes.DoNothing);
        }
    }
}