using ByteSync.Common.Business.Misc;

namespace ByteSync.Services.Applications;

public interface IMsixPfnParser
{
    bool TryParse(string containerDirectoryName, out string? packageFamilyName);
    
    bool TryDetect(string applicationLauncherFullName,
        IEnumerable<string> programsDirectoriesCandidates,
        out DeploymentModes deploymentMode,
        out string? msixPackageFamilyName);
}