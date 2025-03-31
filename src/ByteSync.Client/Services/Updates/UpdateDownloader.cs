using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using ByteSync.Business.Updates;
using ByteSync.Interfaces.Repositories.Updates;
using ByteSync.Interfaces.Updates;

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

        if (string.IsNullOrWhiteSpace(fileUri))
        {
            throw new ArgumentException("The file URI cannot be empty.", nameof(fileUri));
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("The destination path cannot be empty.", nameof(destinationPath));
        }

        await HandleDownloadAsync(cancellationToken, httpClient, fileUri, destinationPath);

        DisposeClient(httpClient);
        
        await CheckDownloadAsync(cancellationToken);
    }

    private async Task HandleDownloadAsync(CancellationToken cancellationToken, HttpClient httpClient, string fileUri, string destinationPath)
    {
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
            
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);

            var totalRead = 0L;
            var buffer = new byte[8192];
            int bytesRead;
            int lastProgress = 0;
            
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalRead += bytesRead;

                if (canReportProgress)
                {
                    int currentProgress = (int)(totalRead * 100 / totalBytes);
                    if (currentProgress - lastProgress >= 1 || currentProgress == 100)
                    {
                        _updateRepository.ReportProgress(new UpdateProgress(UpdateProgressStatus.Downloading, currentProgress));
                        lastProgress = currentProgress;
                    }
                }
            }
            
            if (canReportProgress && lastProgress < 100)
            {
                _updateRepository.ReportProgress(new UpdateProgress(UpdateProgressStatus.Downloading, 100));
            }
            
            _logger.LogInformation("UpdateDownloader: Downloaded file to {destinationPath}", destinationPath);
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
    }

    private void DisposeClient(HttpClient httpClient)
    {
        try
        {
            httpClient.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing HttpClient");
        }
    }

    private async Task CheckDownloadAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        
        string sha256 = await ComputeSha256Async(_updateRepository.UpdateData.DownloadLocation);
        bool isValid = sha256.Equals(_updateRepository.UpdateData.SoftwareVersionFile.PortableZipSha256, StringComparison.OrdinalIgnoreCase);

        if (isValid)
        {
            _logger.LogInformation("UpdateDownloader: Downloaded file checksum is valid");
        }
        else
        {
            throw new InvalidOperationException("Downloaded file checksum is invalid.");
        }
    }

    private async Task<string> ComputeSha256Async(string filePath)
    {
        using var sha256 = SHA256.Create();
        await using var fileStream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(fileStream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}