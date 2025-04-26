using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class TrueExpression : FilterExpression
{
    public override bool Evaluate(ComparisonItem item) => true;
}