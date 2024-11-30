using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Sessions;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface ISharedFilesRepository : IRepository<SharedFileData>
{
    Task AddOrUpdate(SharedFileDefinition sharedFileDefinition, Func<SharedFileData?, SharedFileData> updateHandler);

    Task Forget(SharedFileDefinition sharedFileDefinition);

    Task<List<SharedFileData>> Clear(string sessionId);
}