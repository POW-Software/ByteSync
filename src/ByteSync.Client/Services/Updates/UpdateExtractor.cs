using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ByteSync.Common.Business.Versions;
using ByteSync.Common.Controls;
using ByteSync.Interfaces.Repositories.Updates;
using ByteSync.Interfaces.Updates;

namespace ByteSync.Services.Updates;

public class UpdateExtractor : IUpdateExtractor
{
    private readonly IUpdateRepository _updateRepository;
    private readonly ILogger<UpdateExtractor> _logger;

    public UpdateExtractor(IUpdateRepository updateRepository, ILogger<UpdateExtractor> logger)
    {
        _updateRepository = updateRepository;
        _logger = logger;
    }

    public async Task ExtractAsync()
    {
        var softwareVersionFile = _updateRepository.UpdateData.SoftwareVersionFile;
        var downloadLocation = _updateRepository.UpdateData.DownloadLocation;
        var unzipLocation = _updateRepository.UpdateData.UnzipLocation;

        bool canExtract = softwareVersionFile.Platform switch
        {
            Platform.Windows when softwareVersionFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) =>
                await ExtractWindowsAsync(downloadLocation, unzipLocation),

            Platform.Osx when softwareVersionFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) =>
                await ExtractMacOsAsync(downloadLocation, unzipLocation),

            Platform.Linux when softwareVersionFile.FileName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) =>
                await ExtractLinuxAsync(downloadLocation, unzipLocation),

            _ => false
        };

        if (!canExtract)
        {
            throw new InvalidOperationException("Cannot extract the archive with the provided parameters.");
        }
    }

    private async Task<bool> ExtractWindowsAsync(string downloadLocation, string unzipLocation)
    {
        try
        {
            _logger.LogInformation("UpdateExtractor: Extracting {DownloadLocation} to {UnzipLocation} for Windows", downloadLocation, unzipLocation);

            await Task.Run(() => ZipFile.ExtractToDirectory(downloadLocation, unzipLocation, overwriteFiles: true));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting ZIP archive for Windows");
            return false;
        }
    }

    private async Task<bool> ExtractMacOsAsync(string downloadLocation, string unzipLocation)
    {
        try
        {
            _logger.LogInformation("UpdateExtractor: Extracting {DownloadLocation} to {UnzipLocation} for macOS", downloadLocation, unzipLocation);

            if (UnixHelper.CommandExists("unzip"))
            {
                await GetCommandRunner().RunCommandAsync("unzip", $"-qq \"{downloadLocation}\" -d \"{unzipLocation}\"");
            }
            else
            {
                await Task.Run(() => ZipFile.ExtractToDirectory(downloadLocation, unzipLocation, overwriteFiles: true));
                ApplyPermission(unzipLocation);
            }

            var byteSyncAppPath = Path.Combine(unzipLocation, "ByteSync.app");
            var contentsPath = Path.Combine(byteSyncAppPath, "Contents");
            var destinationPath = Path.Combine(unzipLocation, "Contents");

            if (Directory.Exists(contentsPath))
            {
                Directory.Move(contentsPath, destinationPath);
                Directory.Delete(byteSyncAppPath, recursive: true);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting archive for macOS");
            return false;
        }
    }

    private async Task<bool> ExtractLinuxAsync(string downloadLocation, string unzipLocation)
    {
        try
        {
            _logger.LogInformation("UpdateExtractor: Extracting {DownloadLocation} to {UnzipLocation} for Linux", downloadLocation, unzipLocation);

            await using var fileStream = new FileStream(downloadLocation, FileMode.Open, FileAccess.Read);
            await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            await using var tarReader = new TarReader(gzipStream);

            while (await tarReader.GetNextEntryAsync() is { } entry)
            {
                var destinationPath = Path.Combine(unzipLocation, entry.Name);

                switch (entry.EntryType)
                {
                    case TarEntryType.Directory:
                        Directory.CreateDirectory(destinationPath);
                        break;
                    case TarEntryType.RegularFile:
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                        await entry.ExtractToFileAsync(destinationPath, false);
                        break;
                    default:
                        _logger.LogWarning("Unsupported entry type {EntryType} for {EntryName}", entry.EntryType, entry.Name);
                        break;
                }
            }

            ApplyPermission(unzipLocation);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting TAR.GZ archive for Linux");
            return false;
        }
    }

    private void ApplyPermission(string unzipLocation)
    {
        if (UnixHelper.CommandExists("chmod"))
        {
            var byteSyncFile = Directory.GetFiles(unzipLocation, "ByteSync", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTime)
                .FirstOrDefault();
            if (byteSyncFile != null)
            {
                _logger.LogInformation("UpdateExtractor: Applying execute permissions to {Path}", byteSyncFile);
                GetCommandRunner().RunCommand("chmod", $"+x \"{byteSyncFile}\"");
            }
            else
            {
                _logger.LogWarning("ByteSync file not found; cannot update permissions");
            }
        }
        else
        {
            _logger.LogWarning("The 'chmod' command is missing; cannot update permissions");
        }
    }

    private CommandRunner GetCommandRunner()
    {
        return new CommandRunner { LogLevel = CommandRunner.LogLevels.Minimal, NeedBash = true };
    }
}