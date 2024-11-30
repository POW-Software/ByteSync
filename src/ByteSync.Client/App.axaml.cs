using Autofac;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ByteSync.Interfaces.Factories;
using ByteSync.Services;
using ByteSync.ViewModels;
using ByteSync.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace ByteSync
{
    public class App : Application
    {
        public override void Initialize()
        {
            try
            {
                AvaloniaXamlLoader.Load(this);
                
                // https://lvcharts.com/docs/Avalonia/2.0.0-beta.700/Overview.Installation
                LiveCharts.Configure(config => 
                        config 
                            // registers SkiaSharp as the library backend
                            // REQUIRED unless you build your own
                            .AddSkiaSharp() 

                            // adds the default supported types
                            // OPTIONAL but highly recommend
                            .AddDefaultMappers() 

                            // select a theme, default is Light
                            // OPTIONAL
                            //.AddDarkTheme()
                            .AddLightTheme()
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var container = ContainerProvider.Container;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (container != null)
            {
                var bootstrapperFactory = ContainerProvider.Container.Resolve<IBootstrapperFactory>();
                var bootStrapper = bootstrapperFactory.CreateBootstrapper();
                bootStrapper.AfterFrameworkInitializationCompleted();
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = ContainerProvider.Container.Resolve<MainWindowViewModel>(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
