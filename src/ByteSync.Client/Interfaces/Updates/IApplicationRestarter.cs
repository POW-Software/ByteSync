using System.Threading.Tasks;

namespace ByteSync.Interfaces.Updates;

public interface IApplicationRestarter
{
    string ApplicationLauncherFullName { get; }

    Task StartNewInstance();
    
    Task RestartAndScheduleShutdown(int secondsToWait);
    
    Task Shutdown(int secondsToWait);
    
    void RefreshApplicationLauncherFullName();
}