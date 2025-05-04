using ByteSync.Business.Filtering.Expressions;

namespace ByteSync.Interfaces.Services.Filtering;

public interface IExpressionEvaluatorFactory
{
    public IExpressionEvaluator GetEvaluator(FilterExpression expression);
}