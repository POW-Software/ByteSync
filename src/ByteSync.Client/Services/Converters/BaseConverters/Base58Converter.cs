namespace ByteSync.Services.Converters.BaseConverters;

public class Base58Converter : AbstractBaseConverter
{
    public Base58Converter()
    {
        
    }


    public override string BaseFigures
    {
        get
        {
            // Basé sur https://en.wikipedia.org/wiki/Base62
            return "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        }
    }
}