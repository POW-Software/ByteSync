using ByteSync.Business.Comparisons;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;

class ComparisonElementViewModel : ViewModelBase
{
    private string _description;

    public ComparisonElementViewModel()
    {

    }

    public ComparisonElement ComparisonElement { get; set; }

    [Reactive]
    public string Description { get; set; }

    public bool IsDateOrSize
    {
        get
        {
            return ComparisonElement == ComparisonElement.Date || ComparisonElement == ComparisonElement.Size;
        }
    }
}