using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface IFileDatesSetter
{
    Task SetDates(SharedFileDefinition sharedFileDefinition, string finalDestination, DownloadTargetDates? downloadTargetDates);
    
    Task SetDates(SharedActionsGroup sharedActionsGroup, string finalDestination, DownloadTargetDates? downloadTargetDates);
}