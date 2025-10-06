using System.Text.Json.Serialization;

namespace ByteSync.Business.Sessions;

public class SessionSettings
{
    public SessionSettings()
    {
        AllowedExtensions = new List<string>();
        ForbiddenExtensions = new List<string>();
        
        AnalysisMode = AnalysisModes.Smart;
        DataType = DataTypes.Files;
        MatchingMode = MatchingModes.Tree;
        LinkingCase = LinkingCases.Insensitive;
    }
    
    public bool ExcludeHiddenFiles { get; set; }
    
    public bool ExcludeSystemFiles { get; set; }
    
    public AnalysisModes AnalysisMode { get; set; }
    
    public DataTypes DataType { get; set; }
    
    public MatchingModes MatchingMode { get; set; }
    
    // Backward + forward compat: read and write legacy JSON property name "LinkingKey"
    // Write: emits legacy names ("Name"/"RelativePath") for older clients
    [JsonPropertyName("LinkingKey")]
    [JsonConverter(typeof(LegacyLinkingKeyJsonConverter))]
    public MatchingModes LinkingKey
    {
        get => MatchingMode;
        set => MatchingMode = value;
    }
    
    public LinkingCases LinkingCase { get; set; }
    
    public string? Extensions { get; set; }
    
    public List<string> AllowedExtensions { get; set; }
    
    public List<string> ForbiddenExtensions { get; set; }
    
    public static SessionSettings BuildDefault()
    {
        var cloudSessionSettings = new SessionSettings();
        cloudSessionSettings.DataType = DataTypes.Files;
        cloudSessionSettings.MatchingMode = MatchingModes.Tree;
        cloudSessionSettings.LinkingCase = LinkingCases.Insensitive;
        cloudSessionSettings.ExcludeHiddenFiles = true;
        cloudSessionSettings.ExcludeSystemFiles = true;
        cloudSessionSettings.AnalysisMode = AnalysisModes.Smart;
        
        return cloudSessionSettings;
    }
}