using ByteSync.Assets.Resources;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Services.Misc;

namespace ByteSync.ViewModels.Headers;

public class SoftwareVersionProxy
{
    private readonly ILocalizationService _localizationService;

    public SoftwareVersionProxy(SoftwareVersion softwareVersion, ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        
        SoftwareVersion = softwareVersion;

        Level = SoftwareVersion.Level switch
        {
            PriorityLevel.Minimal => _localizationService[nameof(Resources.SoftwareVersionViewModel_Mandatory)].UppercaseFirst(),
            PriorityLevel.Recommended => _localizationService[nameof(Resources.SoftwareVersionViewModel_Recommended)].UppercaseFirst(),
            PriorityLevel.Optional => _localizationService[nameof(Resources.SoftwareVersionViewModel_Optional)].UppercaseFirst(),
            PriorityLevel.Unknown => "",
            _ => throw new ArgumentOutOfRangeException(nameof(SoftwareVersion.Level), SoftwareVersion.Level, null)
        };
    }

    public SoftwareVersion SoftwareVersion { get; }

    public string Version
    {
        get
        {
            var version = new Version(SoftwareVersion.Version);
            return VersionHelper.GetVersionString(version);
        }
    }

    public string Level { get; }
}