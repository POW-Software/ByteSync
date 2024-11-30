using ByteSync.Business.Comparisons;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;

public class ConditionModeViewModel : ViewModelBase
{
    public ConditionModeViewModel()
    {

    }

    public ConditionModes Mode { get; set; }

    [Reactive]
    public string Description { get; set; }

    public bool IsAny
    {
        get { return Mode == ConditionModes.Any; }
    }

    public bool IsAll
    {
        get { return Mode == ConditionModes.All; }
    }
}