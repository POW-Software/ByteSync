namespace ByteSync.Business.Inventories;

public record InventoryStatistics
{
    public int TotalAnalyzed { get; init; }
    
    public long ProcessedVolume { get; init; }
    
    public int AnalyzeSuccess { get; init; }
    
    public int AnalyzeErrors { get; init; }
    
    public int IdentificationErrors { get; init; }
}
