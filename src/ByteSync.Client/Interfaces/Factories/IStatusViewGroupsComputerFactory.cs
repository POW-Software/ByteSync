using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Interfaces.Factories;

public interface IStatusViewGroupsComputerFactory
{
    IStatusViewGroupsComputer BuildStatusViewGroupsComputer(StatusViewModel statusViewModel);
}