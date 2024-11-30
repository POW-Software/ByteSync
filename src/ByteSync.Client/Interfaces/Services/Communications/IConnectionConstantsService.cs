using System.Threading.Tasks;

namespace ByteSync.Interfaces.Services.Communications;

public interface IConnectionConstantsService
{
    public Task<string> GetApiUrl();

    public TimeSpan[] GetRetriesTimeSpans();
}