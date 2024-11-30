namespace ByteSync.Business.Inventories;

public class BuildingStageData
{
    public long? IdentifiedSize { get; set; }
    public int? IdentifiedFiles { get; set; }
    public int? IdentifiedDirectories { get; set; }
    public int AnalyzedFiles { get; set; }
}