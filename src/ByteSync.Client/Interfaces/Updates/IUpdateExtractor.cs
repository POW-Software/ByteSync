using System.Threading.Tasks;

namespace ByteSync.Interfaces.Updates;

public interface IUpdateExtractor
{
    Task ExtractAsync();
}