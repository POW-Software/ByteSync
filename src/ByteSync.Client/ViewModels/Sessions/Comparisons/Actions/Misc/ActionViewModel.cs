using ByteSync.Common.Business.Actions;
using Prism.Mvvm;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;

class ActionViewModel : BindableBase
{
    public ActionOperatorTypes ActionOperatorType { get; }

    public ActionViewModel(ActionOperatorTypes actionOperatorType, string description)
    {
        ActionOperatorType = actionOperatorType;
        Description = description;
    }

    private string _description;

    public string Description
    {
        get { return _description; }
        set { SetProperty(ref _description, value); }
    }
}