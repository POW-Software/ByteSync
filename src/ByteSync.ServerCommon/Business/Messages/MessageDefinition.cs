namespace ByteSync.ServerCommon.Business.Messages;

public class MessageDefinition
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public Dictionary<string, string> Message { get; set; } = new();
}
