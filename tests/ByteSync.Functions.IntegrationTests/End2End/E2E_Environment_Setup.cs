using System.Diagnostics;
using DotNet.Testcontainers.Builders;
using ContainerBuilder = DotNet.Testcontainers.Builders.ContainerBuilder;
using IContainer = DotNet.Testcontainers.Containers.IContainer;
using FluentAssertions;

namespace ByteSync.Functions.IntegrationTests.End2End;

public class E2E_Environment_Setup
{
    public IContainer Azurite { get; private set; } = null!;
    public IContainer Functions { get; private set; } = null!;
    public HttpClient Http { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Azurite = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite")
            .WithName($"bytesync-azurite-e2e-{Guid.NewGuid():N}")
            .WithPortBinding(10000, 10000)
            .WithPortBinding(10001, 10001)
            .WithPortBinding(10002, 10002)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(10000))
            .Build();

        using (var azuriteCts = new CancellationTokenSource(TimeSpan.FromSeconds(90)))
        {
            await Azurite.StartAsync(azuriteCts.Token);
        }

        string ResolveFunctionsProjectRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ByteSync.sln")))
            {
                dir = dir.Parent;
            }
            if (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "src", "ByteSync.Functions");
                if (Directory.Exists(candidate)) return candidate;
            }
            return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\..\..\src\ByteSync.Functions"));
        }

        var projectRoot = ResolveFunctionsProjectRoot();
        var publishDir = Path.Combine(Path.GetTempPath(), $"bytesync-func-pub-{Guid.NewGuid():N}");
        Directory.CreateDirectory(publishDir);
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{projectRoot}\" -c Release -o \"{publishDir}\"",
            UseShellExecute = false
        };

        using var publish = new Process { StartInfo = startInfo };
        publish.Start();
        await publish.WaitForExitAsync();
        publish.ExitCode.Should().Be(0);

        var cfg = GlobalTestSetup.Configuration;
        var env = new Dictionary<string, string>
        {
            ["AzureWebJobsStorage"] = cfg["AzureWebJobsStorage"]!,
            ["AppSettings__SkipClientsVersionCheck"] = "True" ,
            ["Redis__ConnectionString"] = cfg["Redis:ConnectionString"]!,
            ["SignalR__ConnectionString"] = cfg["SignalR:ConnectionString"]!,
            ["AppSettings__Secret"] = cfg["AppSettings:Secret"]!,
            ["AzureBlobStorage__Endpoint"] = cfg["AzureBlobStorage:Endpoint"]!,
            ["AzureBlobStorage__AccountName"] = cfg["AzureBlobStorage:AccountName"]!,
            ["AzureBlobStorage__AccountKey"] = cfg["AzureBlobStorage:AccountKey"]!,
            ["AzureBlobStorage__Container"] = cfg["AzureBlobStorage:Container"]! 
        };

        var functionsBuilder = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0")
            .WithName($"bytesync-functions-e2e-{Guid.NewGuid():N}")
            .WithPortBinding(7071, 80)
            .WithBindMount(publishDir, "/home/site/wwwroot")
            .WithEnvironment(env)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(
                req => req
                    .ForPort(80)
                    .ForPath("/api/announcements")));

        Functions = functionsBuilder.Build();
        using var funcCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await Functions.StartAsync(funcCts.Token);

        Http = new HttpClient { BaseAddress = new Uri("http://localhost:7071/api/") };
        Http.DefaultRequestHeaders.Add("User-Agent", "ByteSync-E2E-Test");
        await Task.Delay(1000, funcCts.Token);
    }
    
}
