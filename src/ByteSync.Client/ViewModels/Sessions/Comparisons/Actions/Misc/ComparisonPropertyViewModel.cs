using ByteSync.Business.Comparisons;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;

class ComparisonPropertyViewModel : ViewModelBase
{
    private string _description;

    public ComparisonPropertyViewModel()
    {

    }

    public ComparisonProperty ComparisonProperty { get; set; }

    [Reactive]
    public string Description { get; set; }

    public bool IsDateOrSize
    {
        get
        {
            return ComparisonProperty == ComparisonProperty.Date || ComparisonProperty == ComparisonProperty.Size;
        }
    }
}