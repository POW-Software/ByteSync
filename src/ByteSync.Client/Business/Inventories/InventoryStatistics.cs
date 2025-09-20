namespace ByteSync.Business.Inventories;

public record InventoryStatistics
{
    public int TotalAnalyzed { get; init; }
    
    public int Success { get; init; }
    
    public int Errors { get; init; }
}