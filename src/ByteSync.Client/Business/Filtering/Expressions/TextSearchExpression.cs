using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class TextSearchExpression : FilterExpression
{
    private readonly string _searchText;

    public TextSearchExpression(string searchText)
    {
        _searchText = searchText;
    }

    public override bool Evaluate(ComparisonItem item)
    {
        return item.PathIdentity.FileName.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
    }
}