using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Business.Actions;

namespace ByteSync.Business.Actions.Loose;

public class LooseAtomicCondition : IAtomicCondition
{
    public string? SourceName { get; set; }
    
    public string? DestinationName { get; set; }
    
    public int? Size { get; set; }
    
    public ComparisonElement ComparisonElement { get; set; }
    
    public ConditionOperatorTypes ConditionOperator { get; set; }
    
    public DateTime? DateTime { get; set; }

    public SizeUnits? SizeUnit { get; set; }

    public string? NamePattern { get; set; }
}