using System.Threading.Tasks;
using ByteSync.Business.Communications;

namespace ByteSync.Interfaces.Factories;

public interface IConnectionFactory
{
    Task<BuildConnectionResult> BuildConnection();
    
    Task<bool> RefreshAuthenticationTokens();
}