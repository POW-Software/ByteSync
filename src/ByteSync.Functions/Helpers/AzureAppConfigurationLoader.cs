using Azure.Identity;
using ByteSync.Common.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;

namespace ByteSync.Functions.Helpers;

public static class AzureAppConfigurationLoader
{
    public static void AddAzureAppConfiguration(this IConfigurationBuilder builder, HostBuilderContext hostingContext, IConfiguration configuration)
    {
        var azureAppConfigurationUrl = configuration.GetValue<string>("AzureAppConfigurationUrl");

        if (azureAppConfigurationUrl.IsNullOrEmpty())
        {
            return;
        }
        
        Console.WriteLine("Loading configuration from Azure App Configuration...");
        
        builder.AddAzureAppConfiguration(options =>
        {
            var credentials = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeAzureDeveloperCliCredential = true,
                ExcludeEnvironmentCredential = true,
                ExcludeAzurePowerShellCredential = true,
                ExcludeInteractiveBrowserCredential = true,
                ExcludeSharedTokenCacheCredential = true,
            });
            
            options.Connect(new Uri(azureAppConfigurationUrl!), credentials)
                .ConfigureStartupOptions(startupOptions =>
                {
                    startupOptions.Timeout = TimeSpan.FromSeconds(240);
                })
                // Load configuration values with no label
                .Select(KeyFilter.Any, LabelFilter.Null)
                // Override with any configuration values specific to current hosting env
                .Select(KeyFilter.Any, hostingContext.HostingEnvironment.EnvironmentName);

            options.ConfigureKeyVault(kv => kv.SetCredential(credentials));
        });
    }
}