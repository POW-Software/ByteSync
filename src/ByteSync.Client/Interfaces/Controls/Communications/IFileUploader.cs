using System.Threading.Tasks;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFileUploader
{
    Action? ValidateAccess { get; set; }
    Task Upload();
}