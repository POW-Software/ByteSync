using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Updates;
using ByteSync.Common.Helpers;
using PowSoftware.Common.Business.Versions;
using Serilog;

namespace ByteSync.Services.Updates;

public class UpdateDownloader
{
    private WebClient? _webClient;

    public UpdateDownloader(SoftwareVersionFile softwareVersionFile, IProgress<UpdateProgress> progress)
    {
        SyncRoot = new object();
            
        SoftwareVersionFile = softwareVersionFile;

        DownloadFileCompleted = new ManualResetEvent(false);

        Progress = progress;

        ErrorCount = 0;
    }

    private object SyncRoot { get; }
        
    public bool IsCancelled { get; set; }
        
    public bool IsFullyDownloaded { get; set; }
        
    public int ErrorCount { get; set; }
        
    public IProgress<UpdateProgress> Progress { get; set; }

    private ManualResetEvent DownloadFileCompleted { get; }

    public SoftwareVersionFile SoftwareVersionFile { get; }
        
    public string FileToDownload { get; set; }
        
    public string DownloadLocation { get; set; }
        
    public async Task<bool> Download(string fileToDownload, string downloadLocation, CancellationToken cancellationToken)
    {
        // https://github.com/NetSparkleUpdater/NetSparkle/blob/develop/src/NetSparkle/Downloaders/WebClientFileDownloader.cs
        // https://stackoverflow.com/questions/10332506/aborting-a-webclient-downloadfileasync-operation/10332941

        Log.Information("UpdateDownloader: Starting download from '{fileToDownload}' to '{downloadLocation}'", fileToDownload, downloadLocation);
            
        FileToDownload = fileToDownload;
        DownloadLocation = downloadLocation;
            
        DoDownload();

        await Task.Run(() =>
        {
            DownloadFileCompleted.WaitOne();
        });

        return true;
    }

    private void DoDownload()
    {
        Progress.Report(new UpdateProgress(UpdateProgressStatus.Downloading, 0));
            
        if (_webClient != null)
        {
            _webClient.DownloadProgressChanged -= WebClient_DownloadProgressChanged;
            _webClient.DownloadFileCompleted -= WebClient_DownloadFileCompleted;
            // can't re-use WebClient, so cancel old requests
            // and start a new request as needed
            if (_webClient.IsBusy)
            {
                try
                {
                    _webClient.CancelAsync();
                }
                catch
                {
                }
            }
        }

        _webClient = new WebClient
        {
            UseDefaultCredentials = true,
            Proxy = { Credentials = CredentialCache.DefaultNetworkCredentials },
        };

        _webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
        _webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;

        _webClient.DownloadFileAsync(new Uri(FileToDownload, UriKind.Absolute), DownloadLocation);
    }

    private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
        // https://blog.stephencleary.com/2012/02/reporting-progress-from-async-tasks.html
        Progress.Report(new UpdateProgress(UpdateProgressStatus.Downloading, e.ProgressPercentage));
    }

    private void WebClient_DownloadFileCompleted(object? sender, AsyncCompletedEventArgs e)
    {
        bool retryDownload = false;
            
        lock (SyncRoot)
        {
            if (e.Cancelled)
            {
                IsCancelled = true;
                DownloadFileCompleted.Set();
            }
            else if (e.Error != null)
            {
                Log.Error(e.Error, "UpdateDownloader: Error during download");
                ErrorCount += 1;

                if (ErrorCount < 3)
                {
                    retryDownload = true;
                }
                else
                {
                    DownloadFileCompleted.Set();
                }
            }
            else
            {
                IsFullyDownloaded = true;
                Progress.Report(new UpdateProgress(UpdateProgressStatus.Downloading, 100));
                DownloadFileCompleted.Set();
            }
        }

        if (retryDownload)
        {
            DoDownload();
        }
    }

    public async Task CheckDownload()
    {
        if (IsFullyDownloaded)
        {
            string sha256 = CryptographyUtils.ComputeSHA256(DownloadLocation);

            if (sha256.Equals(SoftwareVersionFile.PortableZipSha256, StringComparison.CurrentCultureIgnoreCase))
            {
                    
            } 
        }
    }
}