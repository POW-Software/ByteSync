using ByteSync.Common.Business.Actions;

namespace ByteSync.Common.Interfaces.Business;

public interface IAtomicAction
{
    public string? SourceName { get; }
    
    public string? DestinationName { get; }

    public ActionOperatorTypes Operator { get; set; }
}