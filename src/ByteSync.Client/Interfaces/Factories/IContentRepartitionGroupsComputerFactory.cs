using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Interfaces.Factories;

public interface IContentRepartitionGroupsComputerFactory
{
    IContentRepartitionGroupsComputer Build(ContentRepartitionViewModel contentRepartitionViewModel);
}