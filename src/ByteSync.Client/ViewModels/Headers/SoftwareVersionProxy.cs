using ByteSync.Assets.Resources;
using ByteSync.Common.Business.Versions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Services.Misc;
using Splat;

namespace ByteSync.ViewModels.Headers;

public class SoftwareVersionProxy
{
    private readonly ILocalizationService _localizationService;
    private readonly IEnvironmentService _environmentService;

    public SoftwareVersionProxy(SoftwareVersion softwareVersion, ILocalizationService? localizationService = null,
        IEnvironmentService? environmentService = null)
    {
        _localizationService = localizationService ?? Locator.Current.GetService<ILocalizationService>()!;
        _environmentService = environmentService ?? Locator.Current.GetService<IEnvironmentService>()!;
        
        SoftwareVersion = softwareVersion;

        Level = SoftwareVersion.Level switch
        {
            PriorityLevel.Mandatory => _localizationService[nameof(Resources.SoftwareVersionViewModel_Mandatory)].UppercaseFirst(),
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