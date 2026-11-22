using ByteSync.Business.Actions.Shared;
using ByteSync.Business.DataSources;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface ILocalSourceAuthorizationService
{
    void Grant(DataSource source, bool restoreOnly = false);
    void Revoke(DataSource source);
    void CheckNetworkSource(DataSource source, string sender);
    string Resolve(SharedDataPart part);
    void ValidatePlan(IEnumerable<SharedActionsGroup> groups);
    void ValidatePath(string path);
    void ValidateDestination(SharedActionsGroup group, string destination);
}
