namespace ByteSync.Services.Misc;

public static class VersionHelper
{
    public static string GetVersionString(Version version)
    {
        string versionString = version.Major + "." + version.Minor + "." + version.Build;

        return versionString;
    }
}