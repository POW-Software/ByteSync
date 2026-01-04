using System.Globalization;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Services.Ratings;
using Microsoft.Extensions.Configuration;

namespace ByteSync.Services.Ratings;

public sealed class RatingPromptConfigurationProvider : IRatingPromptConfigurationProvider
{
    private const string RatingPromptSectionName = "RatingPrompt";
    private const string ProbabilityKey = "Probability";
    private const string AdditionalCountKey = "AdditionalCount";
    private const int DefaultAdditionalCount = 3;
    private const double DefaultPromptProbability = 1d / 3d;
    
    private static readonly IReadOnlyDictionary<string, OSPlatforms> StorePlatformMappings =
        new Dictionary<string, OSPlatforms>(StringComparer.OrdinalIgnoreCase)
        {
            ["Windows"] = OSPlatforms.Windows,
            ["Linux"] = OSPlatforms.Linux,
            ["MacOs"] = OSPlatforms.MacOs,
            ["MacOS"] = OSPlatforms.MacOs
        };
    
    public RatingPromptConfigurationProvider(IConfiguration configuration)
    {
        Configuration = BuildConfiguration(configuration.GetSection(RatingPromptSectionName));
    }
    
    public RatingPromptConfiguration Configuration { get; }
    
    private static RatingPromptConfiguration BuildConfiguration(IConfigurationSection section)
    {
        var promptProbability = ReadProbability(section[ProbabilityKey]);
        var additionalCount = ReadAdditionalCount(section[AdditionalCountKey]);
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
        
        return DefaultPromptProbability;
    }
    
    private static int ReadAdditionalCount(string? rawAdditionalCount)
    {
        if (int.TryParse(rawAdditionalCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var additionalCount)
            && additionalCount > 0)
        {
            return additionalCount;
        }
        
        return DefaultAdditionalCount;
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
            if (!StorePlatformMappings.TryGetValue(child.Key, out var osPlatform))
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
        var labelKey = section["LabelKey"]?.Trim();
        var url = section["Url"]?.Trim();
        var icon = section["Icon"]?.Trim();
        
        if (string.IsNullOrWhiteSpace(labelKey)
            || string.IsNullOrWhiteSpace(url)
            || string.IsNullOrWhiteSpace(icon))
        {
            return null;
        }
        
        return new RatingPromptChannelConfiguration(labelKey, url, icon);
    }
}