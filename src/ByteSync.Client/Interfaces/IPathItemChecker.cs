using System.Threading.Tasks;
using ByteSync.Business.PathItems;

namespace ByteSync.Interfaces;

public interface IPathItemChecker
{
    Task<bool> CheckPathItem(PathItem pathItem, IEnumerable<PathItem> existingPathItems);
}