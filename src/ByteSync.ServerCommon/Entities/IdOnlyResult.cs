using Newtonsoft.Json;

namespace ByteSync.ServerCommon.Entities;

public class IdOnlyResult
{
    [JsonProperty("id")]
    public string Id { get; set; }
}