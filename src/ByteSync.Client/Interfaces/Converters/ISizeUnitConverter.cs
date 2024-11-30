using ByteSync.Common.Business.Misc;

namespace ByteSync.Interfaces.Converters;

public interface ISizeUnitConverter
{
    string GetPrintableSizeUnit(SizeUnits? sizeUnit);
}