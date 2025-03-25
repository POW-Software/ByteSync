using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Business.Actions;

namespace ByteSync.Business.Actions.Local;

public class AtomicCondition : IAtomicCondition
{
    public AtomicCondition()
    {

    }
        
    public AtomicCondition(DataPart source, ComparisonElement comparisonElement, ConditionOperatorTypes conditionOperator, DataPart? destination)
    {
        Source = source;
        ComparisonElement = comparisonElement;
        ConditionOperator = conditionOperator;
        Destination = destination;
    }
        
    public DataPart Source { get; set; } = null!;
    public ComparisonElement ComparisonElement { get; set; }
    public ConditionOperatorTypes ConditionOperator { get; set; }
        
    /// <summary>
    /// Peut être nulle quand on travaille sur la Size ou la DateTime
    /// </summary>
    public DataPart? Destination { get; set; }


    public string? SourceName
    {
        get
        {
            return Source.Name;
        }
    }

    public string? DestinationName
    {
        get
        {
            return Destination?.Name;
        }
    }

    public int? Size { get; set; }
    public SizeUnits? SizeUnit { get; set; }
    public DateTime? DateTime { get; set; }
}