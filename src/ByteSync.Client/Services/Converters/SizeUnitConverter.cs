using Autofac;
using Avalonia.Controls;
using ByteSync.Assets.Resources;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Converters;
using ByteSync.Interfaces.Services.Localizations;

namespace ByteSync.Services.Converters;

public class SizeUnitConverter : ISizeUnitConverter
{
    private readonly ILocalizationService _localizationService;

    public SizeUnitConverter()
    {
        if (!Design.IsDesignMode)
        {
            _localizationService = ContainerProvider.Container.Resolve<ILocalizationService>();
        }
    }
    
    public SizeUnitConverter(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }
    
    public string GetPrintableSizeUnit(SizeUnits? sizeUnit)
    {
        if (sizeUnit == null)
        {
            return "";
        }
        
        switch (sizeUnit)
        {
            case SizeUnits.Byte:
                return _localizationService[nameof(Resources.Misc_SizeUnit_Byte)];
            case SizeUnits.KB:
                return _localizationService[nameof(Resources.Misc_SizeUnit_KiloByte)];
            case SizeUnits.MB:
                return _localizationService[nameof(Resources.Misc_SizeUnit_MegaByte)];
            case SizeUnits.GB:
                return _localizationService[nameof(Resources.Misc_SizeUnit_GigaByte)];
            case SizeUnits.TB:
                return _localizationService[nameof(Resources.Misc_SizeUnit_TeraByte)];
            default:
                return "";
        }
    }
}