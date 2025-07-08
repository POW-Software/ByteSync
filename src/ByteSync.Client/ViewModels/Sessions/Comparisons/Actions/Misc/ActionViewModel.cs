using ByteSync.Common.Business.Actions;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;

class ActionViewModel : ViewModelBase
{
    public ActionOperatorTypes ActionOperatorType { get; }

    public ActionViewModel(ActionOperatorTypes actionOperatorType, string description)
    {
        ActionOperatorType = actionOperatorType;
        Description = description;
    }

    private string _description;

    [Reactive]
    public string Description { get; set; }
}