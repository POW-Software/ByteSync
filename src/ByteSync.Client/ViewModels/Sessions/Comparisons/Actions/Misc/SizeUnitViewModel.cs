using ByteSync.Common.Business.Misc;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;

class SizeUnitViewModel
{
    public SizeUnitViewModel(SizeUnits sizeUnit, string shortName)
    {
        SizeUnit = sizeUnit;
        ShortName = shortName;
    }

    public SizeUnits SizeUnit { get; set; }

    public string ShortName { get; set; }
}