namespace ByteSync.Services.Converters.BaseConverters;

public class Base26Converter : AbstractBaseConverter
{
    public override string BaseFigures
    {
        get
        {
            return "abcdefghijklmnopqrstuvwxyz";
        }
    }
}