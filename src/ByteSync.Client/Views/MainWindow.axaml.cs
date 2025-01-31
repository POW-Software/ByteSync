using System.ComponentModel;
using System.Threading.Tasks;
using Autofac;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Services;
using ByteSync.ViewModels;
using Serilog;
using Splat;

namespace ByteSync.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>, IFileDialogService
    {
        private readonly IZoomService _zoomService;

        public MainWindow()
        {
            
        }
        
        public MainWindow(IZoomService zoomService, MainWindowViewModel mainWindowViewModel)
        {
            InitializeComponent();

            _zoomService = zoomService;
            DataContext = mainWindowViewModel;
            
            // Voir ce qui a été fait pour l'enregistrement du FlyoutPanel
            // Locator.CurrentMutable.RegisterConstant<IFileDialogService>(this);
            
            // _zoomService = ContainerProvider.Container.Resolve<IZoomService>();
            
#if DEBUG
            this.AttachDevTools();
            WindowState = WindowState.Normal;

            Width = 1000;
            Height = 650;
#else
            WindowState = WindowState.Maximized;
#endif
            
            PointerWheelChanged += OnPointerWheelChanged;
            IsCtrlDown = false;
        }
        
        private bool CanCloseApplication { get; set; }
        private bool IsCtrlDown { get; set; }

        protected override void OnClosing(CancelEventArgs e)
        {
            // CanCloseApplication indicates if the user has already authorized the end of the application
            if (!CanCloseApplication)
            {
                e.Cancel = true;
                
                // The closing management (messages and treatments) must be done asynchronously
                // This is not compatible with OnClosing (OnClosing does not wait for the end of asynchronous treatments)
                // We launch the method below which is task
                // At the end of this method, CanCloseApplication can be set to True and Close recalled
                TryCloseApplication(ViewModel);
            }

            base.OnClosing(e);
        }

        private Task TryCloseApplication(MainWindowViewModel? mainWindowViewModel)
        {
            return Task.Run(async () =>
            {
                try
                {
                    // ReSharper disable once UnusedVariable C- can be used to debug memory leaks
                    var t = this;

                    bool canCloseApplication;
                    if (mainWindowViewModel != null)
                    {
                        canCloseApplication = await mainWindowViewModel.OnCloseWindowRequested(IsCtrlDown);
                    }
                    else
                    {
                        canCloseApplication = true;
                    }

                    if (canCloseApplication)
                    {
                        CanCloseApplication = true;

                        _ = Dispatcher.UIThread.InvokeAsync(this.Close);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during TryCloseApplication. CanCloseApplication will be set to True");
                    CanCloseApplication = true;
                }
            });
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if ((e.KeyModifiers & KeyModifiers.Control) == KeyModifiers.Control)
            {
                IsCtrlDown = true;
            }
            
            base.OnKeyDown(e);
        }
        
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if ((e.KeyModifiers & KeyModifiers.Control) == KeyModifiers.Control)
            {
                IsCtrlDown = false;
            }
            
            base.OnKeyUp(e);
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                if (e.Delta.Y > 0)
                {
                    _zoomService.ApplicationZoomIn();
                }
                
                else if (e.Delta.Y < 0)
                {
                    _zoomService.ApplicationZoomOut();
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public async Task<string[]?> ShowOpenFileDialogAsync(string title, bool allowMultiple)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = title;
            openFileDialog.AllowMultiple = allowMultiple;

            string[]? result = await openFileDialog.ShowAsync(this);

            return result;
        }

        public async Task<string?> ShowOpenFolderDialogAsync(string title)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            openFolderDialog.Title = title;

            string? result = await openFolderDialog.ShowAsync(this);

            return result;
        }
    }
}
