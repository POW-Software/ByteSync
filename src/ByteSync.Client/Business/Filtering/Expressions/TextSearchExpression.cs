using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class TextSearchExpression : FilterExpression
{
    public string SearchText { get; }

    public TextSearchExpression(string searchText)
    {
        SearchText = searchText;
    }
}