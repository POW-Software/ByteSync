using System.Threading.Tasks;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFileUploader
{
    Task Upload();
}