using Avalonia.Media;
using ReactiveUI;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

public class StatusItemViewModel : ViewModelBase
{
    private Brush? _backBrush;
    private Brush? _foreBrush;
    public string? Letter { get; set; }
    
    public ContentRepartitionViewModel.BrushColors ForeBrushColor { get; set; }

    /// <summary>
    /// Ne pas utiliser [Reactive] ici pour des raisons de performances
    /// L'initialisation se fait par InitBrushes
    /// </summary>
    public Brush? ForeBrush
    {
        get => _foreBrush;
        set => this.RaiseAndSetIfChanged(ref _foreBrush, value);
    }

    public ContentRepartitionViewModel.BrushColors BackBrushColor { get; set; }

    /// <summary>
    /// Ne pas utiliser [Reactive] ici pour des raisons de performances
    /// L'initialisation se fait par InitBrushes
    /// </summary>
    public Brush? BackBrush
    {
        get => _backBrush;
        set => this.RaiseAndSetIfChanged(ref _backBrush, value);
    }

    /// <summary>
    /// Cette méthode permet d'initialiser _foreBrush et _backBrush sans appeler RaiseAndSetIfChanged, ce qui poserait des
    /// problèmes en cas de nombre de ComparisonItemViewModels élevé
    /// </summary>
    /// <param name="foreBrush"></param>
    /// <param name="backBrush"></param>
    public void InitBrushes(Brush? foreBrush, Brush? backBrush)
    {
        _foreBrush = foreBrush;
        _backBrush = backBrush;
    }
}