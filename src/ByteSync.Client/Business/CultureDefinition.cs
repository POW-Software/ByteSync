using System.Globalization;

namespace ByteSync.Business;

public class CultureDefinition
{
    private string _description;

    public CultureDefinition()
    {

    }
        
    public CultureDefinition(CultureInfo cultureInfo)
    {
        Code = cultureInfo.Name;
        Description = cultureInfo.NativeName;
        CultureInfo = cultureInfo;
    }

    public string Description
    {
        get => _description;
        set => _description = value.UppercaseFirst();
    }

    public string Code { get; set; }
        
    public CultureInfo CultureInfo { get; set; }
}