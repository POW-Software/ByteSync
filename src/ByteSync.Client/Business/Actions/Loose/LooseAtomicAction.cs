using ByteSync.Common.Business.Actions;
using ByteSync.Common.Interfaces.Business;

namespace ByteSync.Business.Actions.Loose;

public class LooseAtomicAction : IAtomicAction
{
    public string? SourceName { get; set; }
    
    public string? DestinationName { get; set; }

    public ActionOperatorTypes Operator { get; set; }
}