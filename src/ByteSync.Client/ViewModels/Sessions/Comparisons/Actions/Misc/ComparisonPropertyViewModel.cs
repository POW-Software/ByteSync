using ByteSync.Business.Comparisons;
using Prism.Mvvm;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;

class ComparisonPropertyViewModel : BindableBase
{
    private string _description;

    public ComparisonPropertyViewModel()
    {

    }

    public ComparisonProperty ComparisonProperty { get; set; }

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
            return ComparisonProperty == ComparisonProperty.Date || ComparisonProperty == ComparisonProperty.Size;
        }
    }
}