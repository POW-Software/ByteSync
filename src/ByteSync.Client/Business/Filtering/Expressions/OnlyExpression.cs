using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class OnlyExpression : FilterExpression
{
    public string DataSource { get; }

    public OnlyExpression(string dataSource)
    {
        DataSource = dataSource;
    }
}