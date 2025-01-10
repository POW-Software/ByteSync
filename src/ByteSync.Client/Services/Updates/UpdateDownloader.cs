using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Updates;
using ByteSync.Interfaces.Repositories.Updates;
using ByteSync.Interfaces.Updates;
using Serilog;

namespace ByteSync.Services.Updates;

public class UpdateDownloader : IUpdateDownloader
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUpdateRepository _updateRepository;
    private readonly ILogger<UpdateDownloader> _logger;

    public UpdateDownloader(IHttpClientFactory httpClientFactory, IUpdateRepository updateRepository, ILogger<UpdateDownloader> logger)
    {
        _httpClientFactory = httpClientFactory;
        _updateRepository = updateRepository;
        _logger = logger;
        
    }

    public async Task DownloadAsync(CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("DownloadUpdateClient");
        
        var fileUri = _updateRepository.UpdateData.FileToDownload;
        var destinationPath = _updateRepository.UpdateData.DownloadLocation;
        var progress = _updateRepository.Progress;

        if (string.IsNullOrWhiteSpace(fileUri))
        {
            throw new ArgumentException("The file URI cannot be empty.", nameof(fileUri));
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("The destination path cannot be empty.", nameof(destinationPath));
        }

        try
        {
            using var response = await httpClient.GetAsync(fileUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;

            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            // await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);
            //
            // await contentStream.CopyToAsync(fileStream, 81920, cancellationToken);
            
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);

            var totalRead = 0L;
            var buffer = new byte[8192];
            int bytesRead;
            double lastProgress = 0;
            
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalRead += bytesRead;

                if (canReportProgress)
                {
                    double currentProgress = (double)totalRead / totalBytes * 100;
                    // Éviter de reporter la progression si elle n'a pas changé significativement
                    if (currentProgress - lastProgress >= 1 || currentProgress == 100)
                    {
                        ((IProgress<UpdateProgress>)progress).Report(new UpdateProgress(UpdateProgressStatus.Downloading, (int)currentProgress));
                        lastProgress = currentProgress;
                    }
                }
            }

            // Assurer que la progression atteint 100%
            if (canReportProgress && lastProgress < 100)
            {
                ((IProgress<UpdateProgress>)progress).Report(new UpdateProgress(UpdateProgressStatus.Downloading, 100));
            }
        }
        catch (HttpRequestException httpEx)
        {
            throw new InvalidOperationException($"HTTP request error: {{httpEx.Message}}", httpEx);
        }
        catch (IOException ioEx)
        {
            throw new InvalidOperationException($"File writing error: {{ioEx.Message}}", ioEx);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An unexpected error occurred: {{ex.Message}}", ex);
        }

        try
        {
            httpClient.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing HttpClient");
        }

        // _httpClient.GetAsync()
        //
        // for (; ErrorCount < _maxRetries; ErrorCount++)
        // {
        //     try
        //     {
        //         await DownloadFileAsync(_updateRepository.UpdateData.FileToDownload, _updateRepository.UpdateData.DownloadLocation, Progress, cancellationToken);
        //         IsFullyDownloaded = true;
        //         Log.Information("Download completed successfully.");
        //         break;
        //     }
        //     catch (OperationCanceledException)
        //     {
        //         IsCancelled = true;
        //         Log.Warning("Download was cancelled.");
        //         return false;
        //     }
        //     catch (Exception ex)
        //     {
        //         Log.Error(ex, "UpdateDownloader: Error during download attempt {Attempt}", ErrorCount + 1);
        //         if (ErrorCount + 1 >= _maxRetries)
        //         {
        //             Log.Error("Maximum retry attempts reached.");
        //             return false;
        //         }
        //         Log.Information("Retrying download (Attempt {Attempt})...", ErrorCount + 2);
        //     }
        // }
        //
        // return IsFullyDownloaded;
    }

    // private async Task DownloadFileAsync(string url, string destinationPath, IProgress<UpdateProgress> progress, CancellationToken cancellationToken)
    // {
    //     using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    //     response.EnsureSuccessStatusCode();
    //
    //     var totalBytes = response.Content.Headers.ContentLength ?? -1L;
    //     var canReportProgress = totalBytes != -1 && progress != null;
    //
    //     using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
    //     await ProcessContentStream(totalBytes, contentStream, destinationPath, progress, canReportProgress, cancellationToken);
    // }

    // private async Task ProcessContentStream(long totalDownloadSize, Stream contentStream, string destinationPath, IProgress<UpdateProgress> progress, bool canReportProgress, CancellationToken cancellationToken)
    // {
    //     const int bufferSize = 81920; // 80KB
    //     var totalBytesRead = 0L;
    //     var buffer = new byte[bufferSize];
    //     var isMoreToRead = true;
    //
    //     Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
    //
    //     using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true);
    //
    //     do
    //     {
    //         var bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
    //         if (bytesRead == 0)
    //         {
    //             isMoreToRead = false;
    //             TriggerProgress(progress, totalDownloadSize, totalBytesRead, 100);
    //             continue;
    //         }
    //
    //         await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
    //         totalBytesRead += bytesRead;
    //
    //         if (canReportProgress)
    //         {
    //             var progressPercentage = (int)((totalBytesRead * 100L) / totalDownloadSize);
    //             TriggerProgress(progress, totalDownloadSize, totalBytesRead, progressPercentage);
    //         }
    //
    //     } while (isMoreToRead);
    // }
    //
    // private void TriggerProgress(IProgress<UpdateProgress> progress, long totalDownloadSize, long totalBytesRead, int progressPercentage)
    // {
    //     progress?.Report(new UpdateProgress(UpdateProgressStatus.Downloading, progressPercentage));
    // }

    public async Task CheckDownloadAsync()
    {
        // if (!IsFullyDownloaded)
        //     return false;

        string sha256 = await ComputeSHA256Async(_updateRepository.UpdateData.DownloadLocation);
        bool isValid = sha256.Equals(_updateRepository.UpdateData.SoftwareVersionFile.PortableZipSha256, StringComparison.OrdinalIgnoreCase);

        if (isValid)
        {
            _logger.LogInformation("Downloaded file checksum is valid");
        }
        else
        {
            throw new InvalidOperationException("Downloaded file checksum is invalid.");
        }
    }

    private async Task<string> ComputeSHA256Async(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(fileStream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}