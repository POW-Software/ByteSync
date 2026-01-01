using Avalonia.Media;
using ReactiveUI;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

public class StatusItemViewModel : ViewModelBase
{
    public const double BaseWidth = 24;
    
    private IBrush? _backBrush;
    private IBrush? _foreBrush;
    public string? Letter { get; set; }
    
    public ContentRepartitionViewModel.BrushColors ForeBrushColor { get; set; }

    /// <summary>
    /// Do not use [Reactive] here for performance reasons
    /// Initialization is performed by InitBrushes
    /// </summary>
    public IBrush? ForeBrush
    {
        get => _foreBrush;
        set => this.RaiseAndSetIfChanged(ref _foreBrush, value);
    }

    public ContentRepartitionViewModel.BrushColors BackBrushColor { get; set; }

    /// <summary>
    /// Do not use [Reactive] here for performance reasons
    /// Initialization is performed by InitBrushes
    /// </summary>
    public IBrush? BackBrush
    {
        get => _backBrush;
        set => this.RaiseAndSetIfChanged(ref _backBrush, value);
    }

    /// <summary>
    /// This method allows you to initialize _foreBrush and _backBrush without calling RaiseAndSetIfChanged, which would cause
    /// problems if you have a large number of ComparisonItemViewModels.
    /// </summary>
    /// <param name="foreBrush"></param>
    /// <param name="backBrush"></param>
    public void InitBrushes(IBrush? foreBrush, IBrush? backBrush)
    {
        _foreBrush = foreBrush;
        _backBrush = backBrush;
    }
    
    public double Width { get; set; } = BaseWidth;
}
