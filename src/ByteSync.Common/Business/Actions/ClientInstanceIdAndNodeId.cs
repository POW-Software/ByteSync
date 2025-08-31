namespace ByteSync.Common.Business.Actions;

public record ClientInstanceIdAndNodeId
{
    public string ClientInstanceId { get; init; } = null!;
    public string? NodeId { get; init; }
}


