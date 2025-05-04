using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class FileSystemTypeExpression : FilterExpression
{
    public FileSystemTypes FileSystemType { get; }

    public FileSystemTypeExpression(FileSystemTypes fileSystemType)
    {
        FileSystemType = fileSystemType;
    }
}