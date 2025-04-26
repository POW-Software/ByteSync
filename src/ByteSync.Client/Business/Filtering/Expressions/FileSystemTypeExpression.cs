using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class FileSystemTypeExpression : FilterExpression
{
    private readonly FileSystemTypes _fileSystemType;

    public FileSystemTypeExpression(FileSystemTypes fileSystemType)
    {
        _fileSystemType = fileSystemType;
    }

    public override bool Evaluate(ComparisonItem item)
    {
        return item.FileSystemType == _fileSystemType;
    }
}