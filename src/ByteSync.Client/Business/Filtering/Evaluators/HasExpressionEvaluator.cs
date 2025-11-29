using ByteSync.Business.Filtering.Expressions;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class HasExpressionEvaluator : ExpressionEvaluator<HasExpression>
{
    public override bool Evaluate(HasExpression expression, ComparisonItem item)
    {
        return expression.ExpressionType switch
        {
            HasExpressionType.AccessIssue => EvaluateAccessIssue(item),
            HasExpressionType.ComputationError => EvaluateComputationError(item),
            HasExpressionType.SyncError => EvaluateSyncError(item),
            _ => throw new ArgumentException($"Unknown HasExpressionType: {expression.ExpressionType}")
        };
    }

    private bool EvaluateAccessIssue(ComparisonItem item)
    {
        return item.ContentIdentities.Any(ci => ci.HasAccessIssue);
    }

    private bool EvaluateComputationError(ComparisonItem item)
    {
        return item.ContentIdentities.Any(ci => ci.HasAnalysisError);
    }

    private bool EvaluateSyncError(ComparisonItem item)
    {
        return item.ItemSynchronizationStatus.IsErrorStatus;
    }
}

