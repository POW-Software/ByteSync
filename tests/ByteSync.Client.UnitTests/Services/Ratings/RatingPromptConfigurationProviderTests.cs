using System.Text;
using ByteSync.Common.Business.Misc;
using ByteSync.Services.Ratings;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Ratings;

public class RatingPromptConfigurationProviderTests
{
    [Test]
    public void Builds_configuration_with_valid_values()
    {
        var json = """
                   {
                     "RatingPrompt": {
                       "Probability": 0.5,
                       "AdditionalCount": 2,
                       "AlwaysInclude": [
                         {
                           "LabelKey": "RatingPrompt_Channel_GitHub",
                           "Url": "https://github.com/POW-Software/ByteSync",
                           "Icon": "LogosGithub"
                         }
                       ],
                       "Stores": {
                         "Windows": {
                           "LabelKey": "RatingPrompt_Channel_MicrosoftStore",
                           "Url": "https://apps.microsoft.com/detail/9p17gqw3z2q2?hl=fr-FR&gl=FR",
                           "Icon": "RegularStore"
                         }
                       },
                       "Additional": [
                         {
                           "LabelKey": "RatingPrompt_Channel_AlternativeTo",
                           "Url": "https://alternativeto.net/software/bytesync/about/",
                           "Icon": "RegularWorld"
                         },
                         {
                           "LabelKey": "RatingPrompt_Channel_MajorGeeks",
                           "Url": "https://www.majorgeeks.com/files/details/bytesync.html",
                           "Icon": "RegularWorld"
                         }
                       ]
                     }
                   }
                   """;
        
        var provider = new RatingPromptConfigurationProvider(BuildConfiguration(json));
        
        provider.Configuration.PromptProbability.Should().Be(0.5);
        provider.Configuration.AdditionalCount.Should().Be(2);
        provider.Configuration.AlwaysInclude.Should().ContainSingle();
        provider.Configuration.Additional.Should().HaveCount(2);
        provider.Configuration.GetStoreChannel(OSPlatforms.Windows)!.Url.Should()
            .Contain("apps.microsoft.com");
    }
    
    [Test]
    public void Falls_back_when_values_are_invalid()
    {
        var json = """
                   {
                     "RatingPrompt": {
                       "Probability": 2.5,
                       "AdditionalCount": 0,
                       "AlwaysInclude": [
                         {
                           "LabelKey": "",
                           "Url": "https://github.com/POW-Software/ByteSync",
                           "Icon": "LogosGithub"
                         }
                       ],
                       "Stores": {
                         "Windows": {
                           "LabelKey": "RatingPrompt_Channel_MicrosoftStore",
                           "Url": "",
                           "Icon": "RegularStore"
                         }
                       },
                       "Additional": [
                         {
                           "LabelKey": "RatingPrompt_Channel_AlternativeTo",
                           "Url": " ",
                           "Icon": "RegularWorld"
                         }
                       ]
                     }
                   }
                   """;
        
        var provider = new RatingPromptConfigurationProvider(BuildConfiguration(json));
        
        provider.Configuration.PromptProbability.Should().Be(1d / 3d);
        provider.Configuration.AdditionalCount.Should().Be(3);
        provider.Configuration.AlwaysInclude.Should().BeEmpty();
        provider.Configuration.Additional.Should().BeEmpty();
        provider.Configuration.Stores.Should().BeEmpty();
    }
    
    private static IConfiguration BuildConfiguration(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        
        return new ConfigurationBuilder().AddJsonStream(stream).Build();
    }
}