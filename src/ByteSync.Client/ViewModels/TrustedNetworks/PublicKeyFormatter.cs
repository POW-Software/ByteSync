using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Helpers;
using ByteSync.Services.Converters.BaseConverters;

namespace ByteSync.ViewModels.TrustedNetworks;

public class PublicKeyFormatter
{
    public string Format(PublicKeyCheckData publicKeyCheckData)
    {
        return Format(publicKeyCheckData.IssuerPublicKeyInfo.PublicKey);
    }

    public string Format(byte[] publicKey)
    {
        var md5 = CryptographyUtils.ComputeSHA1(publicKey);
        
        var base58Converter = new Base58Converter();
        var base58 = base58Converter.ConvertTo(md5);

        var result = base58.MultiInsert("\u00A0", 5, 9, 13, 17, 21); // non breaking space

        return result;
    }
}