using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class OnlyExpression : FilterExpression
{
    private readonly string _dataSource;

    public OnlyExpression(string dataSource)
    {
        _dataSource = dataSource;
    }

    public override bool Evaluate(ComparisonItem item)
    {
        var inventories = item.ContentIdentities
            .SelectMany(ci => ci.GetInventories())
            .ToHashSet();

        return inventories.Count == 1 && inventories.First().Letter.Equals(_dataSource, StringComparison.OrdinalIgnoreCase);
    }
}