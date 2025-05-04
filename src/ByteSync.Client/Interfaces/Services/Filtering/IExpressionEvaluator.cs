using ByteSync.Business.Filtering.Expressions;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Interfaces.Services.Filtering;

public interface IExpressionEvaluator
{
    bool Evaluate(FilterExpression expression, ComparisonItem item);
}

public interface IExpressionEvaluator<T> : IExpressionEvaluator where T : FilterExpression
{
    bool Evaluate(T expression, ComparisonItem item);
}