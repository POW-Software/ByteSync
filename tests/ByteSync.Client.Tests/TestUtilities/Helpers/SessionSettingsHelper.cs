using ByteSync.Business.Sessions;

namespace ByteSync.Tests.TestUtilities.Helpers;

public static class SessionSettingsHelper
{
    public static SessionSettings BuildDefaultSessionSettings(
        DataTypes dataType, LinkingKeys linkingKey, 
        AnalysisModes analysisMode = AnalysisModes.Smart)
    {
        SessionSettings sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = analysisMode;
        sessionSettings.DataType = dataType;
        sessionSettings.LinkingKey = linkingKey;
        sessionSettings.LinkingCase = LinkingCases.Insensitive;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;

        return sessionSettings;
    }
}