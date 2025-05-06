using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class ExistsExpression : FilterExpression
{
    public string DataSource { get; }

    public ExistsExpression(string dataSource)
    {
        DataSource = dataSource;
    }
}