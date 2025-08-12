using ByteSync.Business.Comparisons;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;

public class SourceOrPropertyViewModel : ViewModelBase
{
    private string _displayName;
    private string _description;

    public SourceOrPropertyViewModel()
    {
    }

    public SourceOrPropertyViewModel(DataPart dataPart)
    {
        DataPart = dataPart;
        IsDataPart = true;
        DisplayName = dataPart.Name;
        Description = dataPart.Name;
    }

    public SourceOrPropertyViewModel(ComparisonProperty comparisonProperty, string description)
    {
        ComparisonProperty = comparisonProperty;
        IsDataPart = false;
        DisplayName = description;
        Description = description;
    }

    public DataPart? DataPart { get; set; }
    
    public ComparisonProperty? ComparisonProperty { get; set; }
    
    public bool IsDataPart { get; set; }
    
    public bool IsProperty => !IsDataPart;

    [Reactive]
    public string DisplayName { get; set; }

    [Reactive]
    public string Description { get; set; }

    public bool IsNameProperty
    {
        get => IsProperty && ComparisonProperty == Business.Comparisons.ComparisonProperty.Name;
    }

    protected bool Equals(SourceOrPropertyViewModel other)
    {
        if (IsDataPart != other.IsDataPart)
            return false;

        if (IsDataPart)
        {
            return Equals(DataPart, other.DataPart);
        }
        else
        {
            return ComparisonProperty == other.ComparisonProperty;
        }
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SourceOrPropertyViewModel)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = IsDataPart.GetHashCode();
            if (IsDataPart)
                hashCode = (hashCode * 397) ^ (DataPart != null ? DataPart.GetHashCode() : 0);
            else
                hashCode = (hashCode * 397) ^ (ComparisonProperty != null ? ComparisonProperty.GetHashCode() : 0);
            return hashCode;
        }
    }
} 