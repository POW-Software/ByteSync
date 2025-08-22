using System.Reactive.Linq;
using System.Threading;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.Transfers.Downloading;

public class FileDownloaderCache : IFileDownloaderCache
{
    private readonly ISessionService _sessionService;
    private readonly IFileDownloaderFactory _fileDownloaderFactory;
    
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Action<SharedFileDefinition, IDownloadPartsCoordinator>? OnPartsCoordinatorCreated { get; set; }

    public FileDownloaderCache(ISessionService sessionService, IFileDownloaderFactory fileDownloaderFactory)
    {
        _sessionService = sessionService;
        _fileDownloaderFactory = fileDownloaderFactory;

        FileDownloadersDictionary = new Dictionary<string, IFileDownloader>();

        _sessionService.SessionStatusObservable
            .DistinctUntilChanged()
            .Where(s => s == SessionStatus.Preparation)
            .SelectMany(_ => Observable.FromAsync(Reset))
            .Subscribe();
        
        _sessionService.SessionObservable
            .DistinctUntilChanged()
            .Where(s => s == null)
            .SelectMany(_ => Observable.FromAsync(Reset))
            .Subscribe();
    }
    private Dictionary<string, IFileDownloader> FileDownloadersDictionary { get; }
    
    public async Task<IFileDownloader> GetFileDownloader(SharedFileDefinition sharedFileDefinition)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!FileDownloadersDictionary.TryGetValue(sharedFileDefinition.Id, out var fileDownloader))
            {
                fileDownloader = _fileDownloaderFactory.Build(sharedFileDefinition);
                FileDownloadersDictionary.Add(sharedFileDefinition.Id, fileDownloader);
                OnPartsCoordinatorCreated?.Invoke(sharedFileDefinition, fileDownloader.PartsCoordinator);
            }

            return fileDownloader;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RemoveFileDownloader(IFileDownloader fileDownloader)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (fileDownloader is FileDownloader concreteDownloader)
            {
                concreteDownloader.CleanupResources();
            }
            FileDownloadersDictionary.Remove(fileDownloader.SharedFileDefinition.Id);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private async Task Reset()
    {
        await _semaphore.WaitAsync();
        try
        {
            FileDownloadersDictionary.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}