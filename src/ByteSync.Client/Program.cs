global using System;
using System.Threading;
using Autofac;
using Avalonia;
using Avalonia.ReactiveUI;
using ByteSync.Business.Arguments;
using ByteSync.DependencyInjection;
using ByteSync.Interfaces.Controls.Bootstrapping;
using ByteSync.Interfaces.Factories;

namespace ByteSync;

static class Program
{
#if WIN
    // Définition #if WIN : https://stackoverflow.com/questions/30153797/c-sharp-preprocessor-differentiate-between-operating-systems
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int pid);
#endif
        
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains(RegularArguments.WAIT_AFTER_RESTART))
        {
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
            
        var container = ServiceRegistrar.RegisterComponents();
            
        IBootstrapperFactory bootstrapperFactory = container.Resolve<IBootstrapperFactory>();
        var bootstrapper = bootstrapperFactory.CreateBootstrapper();
            
        SetAttachConsole(bootstrapper);
            
        bootstrapper.Start();
    }

    private static void SetAttachConsole(IBootstrapper bootstrapper)
    {
    #if WIN
        bootstrapper.AttachConsole = () =>
        {
            AttachConsole(-1);
        };
    #endif
    }

    //  Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
    }
}