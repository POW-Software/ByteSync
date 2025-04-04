using ByteSync.Business.Comparisons;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IContentRepartitionGroupsComputer
{
    ContentRepartitionComputeResult Compute();
}