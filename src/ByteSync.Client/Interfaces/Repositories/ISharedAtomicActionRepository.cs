using ByteSync.Business.Actions.Shared;

namespace ByteSync.Interfaces.Repositories;

public interface ISharedAtomicActionRepository : IBaseSourceCacheRepository<SharedAtomicAction, string>
{
    void SetSharedAtomicActions(List<SharedAtomicAction> sharedAtomicActions);
}