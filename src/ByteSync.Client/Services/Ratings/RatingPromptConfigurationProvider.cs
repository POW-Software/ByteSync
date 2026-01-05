using System.Globalization;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Services.Ratings;
using Microsoft.Extensions.Configuration;

namespace ByteSync.Services.Ratings;

public sealed class RatingPromptConfigurationProvider : IRatingPromptConfigurationProvider
{
    private const string RATING_PROMPT_SECTION_NAME = "RatingPrompt";
    private const string PROBABILITY_KEY = "Probability";
    private const string ADDITIONAL_COUNT_KEY = "AdditionalCount";
    private const int DEFAULT_ADDITIONAL_COUNT = 3;
    private const double DEFAULT_PROMPT_PROBABILITY = 1d / 3d;
    
    private static readonly IReadOnlyDictionary<string, OSPlatforms> _storePlatformMappings =
        new Dictionary<string, OSPlatforms>(StringComparer.OrdinalIgnoreCase)
        {
            ["Windows"] = OSPlatforms.Windows,
            ["Linux"] = OSPlatforms.Linux,
            ["MacOs"] = OSPlatforms.MacOs,
            ["MacOS"] = OSPlatforms.MacOs
        };
    
    public RatingPromptConfigurationProvider(IConfiguration configuration)
    {
        Configuration = BuildConfiguration(configuration.GetSection(RATING_PROMPT_SECTION_NAME));
    }
    
    public RatingPromptConfiguration Configuration { get; }
    
    private static RatingPromptConfiguration BuildConfiguration(IConfigurationSection section)
    {
        var promptProbability = ReadProbability(section[PROBABILITY_KEY]);
        var additionalCount = ReadAdditionalCount(section[ADDITIONAL_COUNT_KEY]);
        var alwaysInclude = ReadChannels(section.GetSection("AlwaysInclude"));
        var additional = ReadChannels(section.GetSection("Additional"));
        var stores = ReadStores(section.GetSection("Stores"));
        
        return new RatingPromptConfiguration(promptProbability, additionalCount, alwaysInclude, additional, stores);
    }
    
    private static double ReadProbability(string? rawProbability)
    {
        if (double.TryParse(rawProbability, NumberStyles.Float, CultureInfo.InvariantCulture, out var probability)
            && probability >= 0d && probability <= 1d)
        {
            return probability;
        }
        
        return DEFAULT_PROMPT_PROBABILITY;
    }
    
    private static int ReadAdditionalCount(string? rawAdditionalCount)
    {
        if (int.TryParse(rawAdditionalCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var additionalCount)
            && additionalCount > 0)
        {
            return additionalCount;
        }
        
        return DEFAULT_ADDITIONAL_COUNT;
    }
    
    private static IReadOnlyList<RatingPromptChannelConfiguration> ReadChannels(IConfigurationSection section)
    {
        var channels = new List<RatingPromptChannelConfiguration>();
        foreach (var child in section.GetChildren())
        {
            var channel = ReadChannel(child);
            if (channel != null)
            {
                channels.Add(channel);
            }
        }
        
        return channels;
    }
    
    private static IReadOnlyDictionary<OSPlatforms, RatingPromptChannelConfiguration> ReadStores(IConfigurationSection section)
    {
        var stores = new Dictionary<OSPlatforms, RatingPromptChannelConfiguration>();
        foreach (var child in section.GetChildren())
        {
            if (!_storePlatformMappings.TryGetValue(child.Key, out var osPlatform))
            {
                continue;
            }
            
            var channel = ReadChannel(child);
            if (channel != null)
            {
                stores[osPlatform] = channel;
            }
        }
        
        return stores;
    }
    
    private static RatingPromptChannelConfiguration? ReadChannel(IConfigurationSection section)
    {
        var url = section["Url"]?.Trim();
        var icon = section["Icon"]?.Trim();
        var labels = ReadLabels(section.GetSection("Labels"));
        
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(icon))
        {
            return null;
        }
        
        return new RatingPromptChannelConfiguration(url, icon, labels);
    }
    
    private static IReadOnlyDictionary<string, string> ReadLabels(IConfigurationSection section)
    {
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in section.GetChildren())
        {
            var value = child.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                labels[child.Key] = value;
            }
        }
        
        return labels;
    }
}