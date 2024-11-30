using ByteSync.Business.Comparisons;
using Prism.Mvvm;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;

class ComparisonElementViewModel : BindableBase
{
    private string _description;

    public ComparisonElementViewModel()
    {

    }

    public ComparisonElement ComparisonElement { get; set; }

    public string Description
    {
        get => _description;
        set
        {
            SetProperty(ref _description, value);
        }
    }

    public bool IsDateOrSize
    {
        get
        {
            return ComparisonElement == ComparisonElement.Date || ComparisonElement == ComparisonElement.Size;
        }
    }
}