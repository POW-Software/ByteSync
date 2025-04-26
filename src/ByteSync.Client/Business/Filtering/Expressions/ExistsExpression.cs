using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class ExistsExpression : FilterExpression
{
    private readonly string _dataSource;

    public ExistsExpression(string dataSource)
    {
        _dataSource = dataSource;
    }

    public override bool Evaluate(ComparisonItem item)
    {
        var inventories = item.ContentIdentities
            .SelectMany(ci => ci.GetInventories())
            .ToHashSet();

        return inventories.Any(i => i.Letter.Equals(_dataSource, StringComparison.OrdinalIgnoreCase));
    }
}