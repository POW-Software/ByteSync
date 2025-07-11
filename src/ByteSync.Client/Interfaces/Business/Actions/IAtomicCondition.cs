using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Misc;

namespace ByteSync.Interfaces.Business.Actions;

public interface IAtomicCondition
{
    public string? SourceName { get; }

    public string? DestinationName { get; }
    
    public int? Size { get; set; }
    
    public ComparisonElement ComparisonElement { get; set; }
    
    public ConditionOperatorTypes ConditionOperator { get; set; }
    
    public DateTime? DateTime { get; set; }

    public SizeUnits? SizeUnit { get; set; }

    public string? NamePattern { get; set; }
}