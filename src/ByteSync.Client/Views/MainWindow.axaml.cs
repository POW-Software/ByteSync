using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.ViewModels;

namespace ByteSync.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>, IFileDialogService
{
    private readonly IZoomService _zoomService;
    private readonly ILogger<MainWindow> _logger;
    
    public MainWindow()
    {
    }
    
    public MainWindow(IZoomService zoomService, MainWindowViewModel mainWindowViewModel, ILogger<MainWindow> logger)
    {
        InitializeComponent();
        
        _zoomService = zoomService;
        _logger = logger;
        DataContext = mainWindowViewModel;
        
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
        
        Deactivated += OnDeactivated;
    }
    
    private void OnDeactivated(object? sender, EventArgs e)
    {
        // When the window loses focus (e.g. the user switches to another application),
        // this handler checks if a focusable control currently has keyboard focus.
        // If so, it redirects focus to an invisible placeholder (FocusSink) to prevent
        // ScrollViewer from automatically scrolling to the previously focused control
        // when the window regains focus.
        // If Avalonia's focus behavior changes in the future, ensure that FocusSink.Focus()
        // returns true — otherwise the focus may not be properly redirected, and the issue may reappear.
        
        var focused = FocusManager?.GetFocusedElement();
        
        if (focused is InputElement inputElement && inputElement.Focusable)
        {
            FocusSink.Focus();
        }
    }
    
    private bool CanCloseApplication { get; set; }
    
    private bool IsCtrlDown { get; set; }
    
    protected override void OnClosing(WindowClosingEventArgs e)
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
                _logger.LogError(ex, "Error during TryCloseApplication. CanCloseApplication will be set to True");
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
    
    public async Task<string[]?> ShowOpenFileDialogAsync(string title, bool allowMultiple)
    {
        var storageProvider = GetTopLevel(this)?.StorageProvider;
        if (storageProvider == null)
        {
            return null;
        }
        
        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple
        };
        
        var files = await storageProvider.OpenFilePickerAsync(options);
        
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (files != null && files.Count > 0)
        {
            return files.Select(file => file.TryGetLocalPath()).Where(path => path != null)
                .ToArray()!; // NOSONAR
        }
        
        return null;
    }
    
    public async Task<string?> ShowOpenFolderDialogAsync(string title)
    {
        var storageProvider = GetTopLevel(this)?.StorageProvider;
        if (storageProvider == null)
        {
            return null;
        }
        
        var options = new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };
        
        var folders = await storageProvider.OpenFolderPickerAsync(options);
        
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (folders != null && folders.Count > 0)
        {
            return folders[0].TryGetLocalPath(); // NOSONAR
        }
        
        return null;
    }
}