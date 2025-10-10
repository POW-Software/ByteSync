namespace ByteSync.Business.Inventories;

public record InventoryMonitorData
{
    public int IdentifiedFiles { get; set; }
    
    public int IdentifiedDirectories { get; set; }
    
    public int AnalyzedFiles { get; set; }
    
    public long ProcessedVolume { get; set; }
    
    public int AnalyzeErrors { get; set; }
    
    public int AnalyzableFiles { get; set; }
    
    public long AnalyzableVolume { get; set; }
    
    public long IdentifiedVolume { get; set; }
    
    public bool HasNonZeroProperty()
    {
        return IdentifiedFiles != 0
               || IdentifiedDirectories != 0
               || AnalyzedFiles != 0
               || ProcessedVolume != 0
               || AnalyzeErrors != 0
               || AnalyzableFiles != 0
               || AnalyzableVolume != 0
               || IdentifiedVolume != 0;
    }
}