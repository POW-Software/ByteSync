using ByteSync.Business.Filtering.Expressions;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class FileSystemTypeExpressionEvaluator : ExpressionEvaluator<FileSystemTypeExpression>
{
    public override bool Evaluate(FileSystemTypeExpression expression, ComparisonItem item)
    {
        return item.FileSystemType == expression.FileSystemType;
    }
}
