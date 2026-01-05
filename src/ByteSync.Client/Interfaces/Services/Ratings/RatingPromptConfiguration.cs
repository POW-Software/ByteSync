using ByteSync.Common.Business.Misc;

namespace ByteSync.Interfaces.Services.Ratings;

public sealed record RatingPromptChannelConfiguration(
    string Url,
    string Icon,
    IReadOnlyDictionary<string, string> Labels);

public sealed record RatingPromptConfiguration(
    double PromptProbability,
    int AdditionalCount,
    IReadOnlyList<RatingPromptChannelConfiguration> AlwaysInclude,
    IReadOnlyList<RatingPromptChannelConfiguration> Additional,
    IReadOnlyDictionary<OSPlatforms, RatingPromptChannelConfiguration> Stores)
{
    public RatingPromptChannelConfiguration? GetStoreChannel(OSPlatforms osPlatform)
    {
        return Stores.GetValueOrDefault(osPlatform);
    }
}