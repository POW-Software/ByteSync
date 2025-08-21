using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ByteSync.Business.Arguments;
using ByteSync.Common.Controls;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Updates;
using Serilog;

namespace ByteSync.Services.Updates;

public class ApplicationRestarter : IApplicationRestarter
{
    public ApplicationRestarter(IEnvironmentService environmentService)
    {
        // https://stackoverflow.com/questions/69591079/original-assembly-location-in-net-5-0-linux-with-single-file-executable
        // https://stackoverflow.com/questions/33071696/how-to-get-executing-assembly-location
        // var bd = AppDomain.CurrentDomain.BaseDirectory;
        // var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var applicationLauncherFullName = environmentService.AssemblyFullName; // Environment.GetCommandLineArgs()[0];
        if (applicationLauncherFullName.EndsWith(".DLL", StringComparison.CurrentCultureIgnoreCase) &&
            applicationLauncherFullName.Length > 4)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                applicationLauncherFullName = applicationLauncherFullName.Substring(0, applicationLauncherFullName.Length - 4) + ".exe";
            }
            else
            {
                applicationLauncherFullName = applicationLauncherFullName.Substring(0, applicationLauncherFullName.Length - 4);
            }
        }

        ApplicationLauncherFullName = applicationLauncherFullName;
        ExecutableName = new FileInfo(ApplicationLauncherFullName).Name;
    }

    public string ApplicationLauncherFullName { get; private set; }
        
    public string ExecutableName { get; }

    public async Task RestartAndScheduleShutdown(int secondsToWait)
    {
        await StartNewInstance();

        await Shutdown(secondsToWait);
    }

    public Task StartNewInstance()
    {
        Log.Information("ApplicationRestarter: starting a new application instance");
            
        return Task.Run(() =>
        {
            // https://stackoverflow.com/questions/39155571/how-to-run-net-core-console-application-from-the-command-line

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    DoStartNewInstanceRegular();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    bool useOpen = false;
                        
                    // Sur macOS, il existe la commande open qui permet de lancer un .app
                    // On regarde si on peut utiliser cette commande. Pour cela, il faut :
                    //      - Etre dans un répertoire .app
                    //      - Que la commande open soit disponible
                        
                    var applicationLauncher = new FileInfo(ApplicationLauncherFullName);
                    if (applicationLauncher.HasAncestor(d => d.Name.Equals("MacOS", StringComparison.InvariantCultureIgnoreCase))
                        && applicationLauncher.HasAncestor(d => d.Name.Equals("Contents", StringComparison.InvariantCultureIgnoreCase))
                        && applicationLauncher.HasAncestor(d => d.Name.StartsWith("ByteSync", StringComparison.InvariantCultureIgnoreCase)
                                                                && d.Name.EndsWith(".app", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        if (UnixHelper.CommandExists("open"))
                        {
                            useOpen = true;
                        }
                    }
                        
                    if (useOpen)
                    {
                        DoStartNewInstanceWithOpenCommand();
                    }
                    else
                    {
                        DoStartNewInstanceRegular();
                    }
                }
                else
                {
                    throw new Exception("Unknown platform");
                }
                    
                Log.Information("ApplicationRestarter: new application instance start has be requested");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ApplicationRestarter: Error during application restart");
            }
        });
    }

    private void DoStartNewInstanceWithOpenCommand()
    {
        // https://stackoverflow.com/questions/1308755/how-to-launch-an-app-on-os-x-with-command-line-the-best-way
            
        ProcessStartInfo processStartInfo = new ProcessStartInfo();
        processStartInfo.FileName = "/usr/bin/open";
            
        var applicationLauncher = new FileInfo(ApplicationLauncherFullName);
        DirectoryInfo appDirectory = 
            applicationLauncher.FirstAncestor(d => d.Name.StartsWith("ByteSync", StringComparison.InvariantCultureIgnoreCase)
                                                   && d.Name.EndsWith(".app", StringComparison.InvariantCultureIgnoreCase));
            
        StringBuilder arguments = new StringBuilder();
        arguments.Append("-n").Append(' ');
        arguments.Append(appDirectory.FullName).Append(' ');

        arguments.Append("--args").Append(' ');
        var applicationArguments = GetApplicationArguments();
        arguments.Append(applicationArguments);
            
        processStartInfo.Arguments = arguments.ToString();
            
        processStartInfo.UseShellExecute = true;
            
        Log.Information("ApplicationRestarter: launching open -n {FileName}", appDirectory.FullName);
            
        var startedProcess = Process.Start(processStartInfo);
        startedProcess.WaitForExit();
    }

    private void DoStartNewInstanceRegular()
    {
        ProcessStartInfo processStartInfo = new ProcessStartInfo();
  
        processStartInfo.FileName = ApplicationLauncherFullName;
            
        StringBuilder arguments = GetApplicationArguments();;
        processStartInfo.Arguments = arguments.ToString();

        processStartInfo.UseShellExecute = true;

        // processStartInfo.WorkingDirectory = new FileInfo(ApplicationLauncherFullName).Directory.FullName;
            
        Log.Information("ApplicationRestarter: launching {FileName}", processStartInfo.FileName);

        var startedProcess = Process.Start(processStartInfo);
    }
        
    private StringBuilder GetApplicationArguments()
    {
        StringBuilder arguments = new StringBuilder();
        foreach (var argument in Environment.GetCommandLineArgs().Skip(1).Where(a =>
                     !a.Equals(RegularArguments.WAIT_AFTER_RESTART) && !a.Equals(RegularArguments.UPDATE)))
        {
            arguments.Append(argument).Append(' ');
        }

        arguments.Append(RegularArguments.WAIT_AFTER_RESTART);

        return arguments;
    }

    public async Task Shutdown(int secondsToWait)
    {
        if (secondsToWait > 0)
        {
            if (secondsToWait < 2)
            {
                Log.Information("ApplicationRestarter: scheduling the shutdown of this application instance ({secondsToWait} second)", secondsToWait);
            }
            else
            {
                Log.Information("ApplicationRestarter: scheduling the shutdown of this application instance ({secondsToWait} seconds)", secondsToWait);
            }

            await Task.Delay(TimeSpan.FromSeconds(secondsToWait));
        }

        Log.Information("ApplicationRestarter: shutting down this application instance");
        Environment.Exit(0);
    }

    public void RefreshApplicationLauncherFullName()
    {
        FileInfo fileInfo = new FileInfo(ApplicationLauncherFullName);

        bool hasChanged = false;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (!fileInfo.Exists
                && fileInfo.Directory != null 
                && fileInfo.Directory.Name.StartsWith("net", StringComparison.InvariantCultureIgnoreCase)
                && fileInfo.Directory.Parent != null 
                && fileInfo.Directory.Parent.Name.Equals("MacOS", StringComparison.InvariantCultureIgnoreCase))
            {
                fileInfo = new FileInfo(fileInfo.Directory.Parent.Combine(fileInfo.Name));

                if (fileInfo.Exists)
                {
                    hasChanged = true;
                }
            }
        }

        if (hasChanged)
        {
            ApplicationLauncherFullName = fileInfo.FullName; 
            Log.Information("ApplicationRestarter: Application Launcher is now {Path}", ApplicationLauncherFullName);
        }
    }
}