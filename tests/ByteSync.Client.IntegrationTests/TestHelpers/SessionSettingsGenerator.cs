using ByteSync.Business.Sessions;

namespace ByteSync.Client.IntegrationTests.TestHelpers;

public static class SessionSettingsGenerator
{
    public static SessionSettings GenerateSessionSettings(
        DataTypes dataType = DataTypes.FilesDirectories,
        MatchingModes matchingMode = MatchingModes.Tree,
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