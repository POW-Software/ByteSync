using ByteSync.Business.Sessions;

namespace ByteSync.Client.UnitTests.TestUtilities.Helpers;

public static class SessionSettingsHelper
{
    public static SessionSettings BuildDefaultSessionSettings(
        DataTypes dataType, MatchingModes matchingMode,
        AnalysisModes analysisMode = AnalysisModes.Smart)
    {
        var sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = analysisMode;
        sessionSettings.DataType = dataType;
        sessionSettings.MatchingMode = matchingMode;
        sessionSettings.LinkingCase = LinkingCases.Insensitive;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        
        return sessionSettings;
    }
}