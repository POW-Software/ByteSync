namespace ByteSync.Business.Sessions;

public class SessionSettings
{
    public SessionSettings()
    {
        AllowedExtensions = new List<string>();
        ForbiddenExtensions = new List<string>();

        AnalysisMode = AnalysisModes.Smart;
        DataType = DataTypes.Files;
        LinkingKey = LinkingKeys.RelativePath;
        LinkingCase = LinkingCases.Insensitive;
    }
    
    public bool ExcludeHiddenFiles { get; set; }
    
    public bool ExcludeSystemFiles { get; set; }
    
    public AnalysisModes AnalysisMode { get; set; }
    
    public DataTypes DataType { get; set; }
    
    public LinkingKeys LinkingKey { get; set; }
    
    public LinkingCases LinkingCase { get; set; }
    
    public string? Extensions { get; set; }
    
    public List<string> AllowedExtensions { get; set; }
    
    public List<string> ForbiddenExtensions { get; set; }

    public static SessionSettings BuildDefault()
    {
        var cloudSessionSettings = new SessionSettings();
        cloudSessionSettings.DataType = DataTypes.Files;
        cloudSessionSettings.LinkingKey = LinkingKeys.RelativePath;
        cloudSessionSettings.LinkingCase = LinkingCases.Insensitive;
        cloudSessionSettings.ExcludeHiddenFiles = true;
        cloudSessionSettings.ExcludeSystemFiles = true;
        cloudSessionSettings.AnalysisMode = AnalysisModes.Smart;

        return cloudSessionSettings;
    }
}