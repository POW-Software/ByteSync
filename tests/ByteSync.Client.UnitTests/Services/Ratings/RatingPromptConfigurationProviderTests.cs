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
                           "Url": "https://github.com/POW-Software/ByteSync",
                           "Icon": "LogosGithub",
                           "Labels": {
                             "en": "Star on GitHub",
                             "fr": "Mettre une étoile sur GitHub"
                           }
                         }
                       ],
                       "Stores": {
                         "Windows": {
                           "Url": "https://apps.microsoft.com/detail/9p17gqw3z2q2?hl=fr-FR&gl=FR",
                           "Icon": "RegularStore",
                           "Labels": {
                             "en": "Rate on Microsoft Store",
                             "fr": "Noter sur Microsoft Store"
                           }
                         }
                       },
                       "Additional": [
                         {
                           "Url": "https://alternativeto.net/software/bytesync/about/",
                           "Icon": "RegularWorld",
                           "Labels": {
                             "en": "Rate on AlternativeTo",
                             "fr": "Noter sur AlternativeTo"
                           }
                         },
                         {
                           "Url": "https://www.majorgeeks.com/files/details/bytesync.html",
                           "Icon": "RegularWorld",
                           "Labels": {
                             "en": "Rate on MajorGeeks",
                             "fr": "Noter sur MajorGeeks"
                           }
                         }
                       ]
                     }
                   }
                   """;
        
        var provider = new RatingPromptConfigurationProvider(BuildConfiguration(json));
        
        provider.Configuration.PromptProbability.Should().Be(0.5);
        provider.Configuration.AdditionalCount.Should().Be(2);
        provider.Configuration.AlwaysInclude.Should().ContainSingle();
        provider.Configuration.AlwaysInclude[0].Labels.Should().ContainKey("fr");
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
                           "Url": "",
                           "Icon": "LogosGithub",
                           "Labels": {
                             "en": "Star on GitHub"
                           }
                         }
                       ],
                       "Stores": {
                         "Windows": {
                           "Url": "",
                           "Icon": "RegularStore",
                           "Labels": {
                             "en": "Rate on Microsoft Store"
                           }
                         }
                       },
                       "Additional": [
                         {
                           "Url": " ",
                           "Icon": "RegularWorld",
                           "Labels": {
                             "en": "Rate on AlternativeTo"
                           }
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