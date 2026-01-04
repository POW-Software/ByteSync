using ByteSync.Common.Business.Misc;

namespace ByteSync.Interfaces.Services.Ratings;

public sealed record RatingPromptChannelConfiguration(string LabelKey, string Url, string Icon);

public sealed record RatingPromptConfiguration(
    double PromptProbability,
    int AdditionalCount,
    IReadOnlyList<RatingPromptChannelConfiguration> AlwaysInclude,
    IReadOnlyList<RatingPromptChannelConfiguration> Additional,
    IReadOnlyDictionary<OSPlatforms, RatingPromptChannelConfiguration> Stores)
{
    public RatingPromptChannelConfiguration? GetStoreChannel(OSPlatforms osPlatform)
    {
        return Stores.TryGetValue(osPlatform, out var channel) ? channel : null;
    }
}